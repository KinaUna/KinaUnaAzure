using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.AspNetCore.Mvc;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController(IAzureNotifications azureNotifications, IImageStore imageStore, IDataService dataService) : ControllerBase
    {
        [HttpPost]
        public async Task<HttpResponseMessage> Post(string pns, [FromBody] string message, string to_tag)
        {
            string user = User.GetEmail();
            if (user == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            string[] userTag = ["username:" + to_tag, "from:" + user];
            Microsoft.Azure.NotificationHubs.NotificationOutcome notificationOutcome = null;
            HttpStatusCode returnStatusCode = HttpStatusCode.InternalServerError;

            switch (pns.ToLower())
            {
                case "wns":
                    // Windows 8.1 / Windows Phone 8.1
                    string toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" +
                                   "From " + user + ": " + message + "</text></binding></visual></toast>";
                    notificationOutcome = await azureNotifications.Hub.SendWindowsNativeNotificationAsync(toast, userTag);
                    break;
                case "apns":
                    // iOS
                    string alert = "{\"aps\":{\"alert\":\"" + "From " + user + ": " + message + "\"}}";
                    notificationOutcome = await azureNotifications.Hub.SendAppleNativeNotificationAsync(alert, userTag);
                    break;
                case "fcm":
                    // Android
                    string notif = "{ \"data\" : {\"message\":\"" + "From " + user + ": " + message + "\"}}";
                    notificationOutcome = await azureNotifications.Hub.SendFcmNativeNotificationAsync(notif, userTag);
                    break;
            }

            if (notificationOutcome == null) return new HttpResponseMessage(returnStatusCode);

            if (!((notificationOutcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Abandoned) ||
                  (notificationOutcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Unknown)))
            {
                returnStatusCode = HttpStatusCode.OK;
            }

            return new HttpResponseMessage(returnStatusCode);
        }

        [HttpGet]
        [Route("[action]/{count:int}/{start:int}/{language}")]
        public async Task<IActionResult> Latest(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await dataService.GetUsersMobileNotifications(userId, language);
            if (notifications.Count == 0) return Ok(notifications);

            if (start > notifications.Count)
            {
                return Ok(new List<MobileNotification>());
            }

            notifications = [.. notifications.OrderByDescending(n => n.Time)];
            notifications = notifications.Skip(start).Take(count).ToList();
            foreach (MobileNotification notif in notifications)
            {
                if (string.IsNullOrEmpty(notif.IconLink))
                {
                    notif.IconLink = Constants.ProfilePictureUrl;
                }

                notif.IconLink = imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
            }

            return Ok(notifications);
        }

        [HttpGet]
        [Route("[action]/{count:int}/{start:int}/{language}")]
        public async Task<IActionResult> Unread(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await dataService.GetUsersMobileNotifications(userId, language);
            notifications = notifications.Where(n => n.Read == false).ToList();
            if (notifications.Count == 0) return Ok(notifications);

            if (start > notifications.Count)
            {
                return Ok(new List<MobileNotification>());
            }

            notifications = [.. notifications.OrderByDescending(n => n.Time)];
            notifications = notifications.Skip(start).Take(count).ToList();
            foreach (MobileNotification notif in notifications)
            {
                if (string.IsNullOrEmpty(notif.IconLink))
                {
                    notif.IconLink = Constants.ProfilePictureUrl;
                }

                notif.IconLink = imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
            }

            return Ok(notifications);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] MobileNotification value)
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            MobileNotification mobileNotification = await dataService.GetMobileNotification(id);
            if (mobileNotification == null || mobileNotification.UserId != userId) return Ok(value);
            
            mobileNotification.Read = value.Read;
            mobileNotification = await dataService.UpdateMobileNotification(mobileNotification);

            return Ok(mobileNotification);

        }

        // DELETE api/notifications/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            MobileNotification mobileNotification = await dataService.GetMobileNotification(id);
            if (mobileNotification != null)
            {
                if (mobileNotification.UserId == User.GetUserId())
                {
                    _ = await dataService.DeleteMobileNotification(mobileNotification);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddPushDevice([FromBody] PushDevices device)
        {
            //Todo: Add UserId to PushDevice and check if user should have access.

            device = await dataService.AddPushDevice(device);

            return Ok(device);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> RemovePushDevice([FromBody] PushDevices device)
        {
            await dataService.RemovePushDevice(device);

            return Ok(device);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> GetPushDeviceById(int id)
        {
            PushDevices device = await dataService.GetPushDeviceById(id);

            return Ok(device);
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetPushDevicesListByUserId(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<PushDevices> devices = await dataService.GetPushDevicesListByUserId(userId);

            return Ok(devices);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetPushDevice([FromBody] PushDevices device)
        {
            device = await dataService.GetPushDevice(device);

            return Ok(device);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (notification.To != currentUserId)
            {
                return Unauthorized();
            }

            notification = await dataService.AddWebNotification(notification);

            return Ok(notification);
        }

        [HttpPut]
        [Route("[action]")]
        public async Task<IActionResult> UpdateWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (notification.To != currentUserId)
            {
                return Unauthorized();
            }

            notification = await dataService.UpdateWebNotification(notification);

            return Ok(notification);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> RemoveWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (notification.To != currentUserId)
            {
                return Unauthorized();
            }

            await dataService.RemoveWebNotification(notification);

            return Ok(notification);
        }

        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> GetWebNotificationById(int id)
        {
            WebNotification webNotification = await dataService.GetWebNotificationById(id);

            string currentUserId = User.GetUserId() ?? "";
            if (webNotification.To != currentUserId)
            {
                return Unauthorized();
            }

            return Ok(webNotification);
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetUsersNotifications(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<WebNotification> webNotifications = await dataService.GetUsersWebNotifications(userId);

            return Ok(webNotifications);
        }

        [HttpGet]
        [Route("[action]/{userId}/{start:int}/{count:int}/{unreadOnly:bool}")]
        public async Task<IActionResult> GetLatestWebNotifications(string userId, int start, int count, bool unreadOnly)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<WebNotification> notificationsList = await dataService.GetLatestWebNotifications(userId, start, count, unreadOnly);

            return Ok(notificationsList);
        }

        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetUsersNotificationsCount(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            int notificationsCount = await dataService.GetUsersNotificationsCount(userId);

            return Ok(notificationsCount);
        }
    }
}