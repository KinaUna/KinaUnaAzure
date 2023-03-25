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
    public class NotificationsController : ControllerBase
    {
        private readonly IAzureNotifications _azureNotifications;
        private readonly IDataService _dataService;
        private readonly IImageStore _imageStore;

        public NotificationsController(IAzureNotifications azureNotifications, IImageStore imageStore, IDataService dataService)
        {
            _azureNotifications = azureNotifications;
            _dataService = dataService;
            _imageStore = imageStore;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(string pns, [FromBody] string message, string to_tag)
        {
            string user = User.GetEmail();
            if (user == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            string[] userTag = new string[2];
            userTag[0] = "username:" + to_tag;
            userTag[1] = "from:" + user;

            Microsoft.Azure.NotificationHubs.NotificationOutcome outcome = null;
            HttpStatusCode ret = HttpStatusCode.InternalServerError;

            switch (pns.ToLower())
            {
                case "wns":
                    // Windows 8.1 / Windows Phone 8.1
                    string toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" +
                                   "From " + user + ": " + message + "</text></binding></visual></toast>";
                    outcome = await _azureNotifications.Hub.SendWindowsNativeNotificationAsync(toast, userTag);
                    break;
                case "apns":
                    // iOS
                    string alert = "{\"aps\":{\"alert\":\"" + "From " + user + ": " + message + "\"}}";
                    outcome = await _azureNotifications.Hub.SendAppleNativeNotificationAsync(alert, userTag);
                    break;
                case "fcm":
                    // Android
                    string notif = "{ \"data\" : {\"message\":\"" + "From " + user + ": " + message + "\"}}";
                    outcome = await _azureNotifications.Hub.SendFcmNativeNotificationAsync(notif, userTag);
                    break;
            }

            if (outcome != null)
            {
                if (!((outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Abandoned) ||
                      (outcome.State == Microsoft.Azure.NotificationHubs.NotificationOutcomeState.Unknown)))
                {
                    ret = HttpStatusCode.OK;
                }
            }

            return new HttpResponseMessage(ret);
        }

        [HttpGet]
        [Route("[action]/{count}/{start}/{language}")]
        public async Task<IActionResult> Latest(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await _dataService.GetUsersMobileNotifications(userId, language);

            if (notifications.Any())
            {
                if (start > notifications.Count)
                {
                    return Ok(new List<MobileNotification>());
                }
                notifications = notifications.OrderByDescending(n => n.Time).ToList();
                notifications = notifications.Skip(start).Take(count).ToList();
                foreach (MobileNotification notif in notifications)
                {
                    if (string.IsNullOrEmpty(notif.IconLink))
                    {
                        notif.IconLink = Constants.ProfilePictureUrl;
                    }

                    notif.IconLink = _imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
                }
            }

            return Ok(notifications);
        }

        [HttpGet]
        [Route("[action]/{count}/{start}/{language}")]
        public async Task<IActionResult> Unread(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await _dataService.GetUsersMobileNotifications(userId, language);
            notifications = notifications.Where(n => n.Read == false).ToList();

            if (notifications.Any())
            {
                if (start > notifications.Count)
                {
                    return Ok(new List<MobileNotification>());
                }
                notifications = notifications.OrderByDescending(n => n.Time).ToList();
                notifications = notifications.Skip(start).Take(count).ToList();
                foreach (MobileNotification notif in notifications)
                {
                    if (string.IsNullOrEmpty(notif.IconLink))
                    {
                        notif.IconLink = Constants.ProfilePictureUrl;
                    }

                    notif.IconLink = _imageStore.UriFor(notif.IconLink, BlobContainers.Profiles);
                }
            }

            return Ok(notifications);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] MobileNotification value)
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            MobileNotification mobileNotification = await _dataService.GetMobileNotification(id);

            if (mobileNotification != null && mobileNotification.UserId == userId)
            {
                mobileNotification.Read = value.Read;
                mobileNotification = await _dataService.UpdateMobileNotification(mobileNotification);

                return Ok(mobileNotification);
            }

            return Ok(value);
        }

        // DELETE api/notifications/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            MobileNotification mobileNotification = await _dataService.GetMobileNotification(id);
            if (mobileNotification != null)
            {
                if (mobileNotification.UserId == User.GetUserId())
                {
                    _ = await _dataService.DeleteMobileNotification(mobileNotification);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddPushDevice([FromBody] PushDevices device)
        {
            //Todo: Add UserId to PushDevice and check if user should have access.

            device = await _dataService.AddPushDevice(device);

            return Ok(device);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> RemovePushDevice([FromBody] PushDevices device)
        {
            await _dataService.RemovePushDevice(device);

            return Ok(device);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> GetPushDeviceById(int id)
        {
            PushDevices device = await _dataService.GetPushDeviceById(id);

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

            List<PushDevices> devices = await _dataService.GetPushDevicesListByUserId(userId);

            return Ok(devices);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetPushDevice([FromBody] PushDevices device)
        {
            device = await _dataService.GetPushDevice(device);

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

            notification = await _dataService.AddWebNotification(notification);

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

            notification = await _dataService.UpdateWebNotification(notification);

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

            await _dataService.RemoveWebNotification(notification);

            return Ok(notification);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> GetWebNotificationById(int id)
        {
            WebNotification webNotification = await _dataService.GetWebNotificationById(id);

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

            List<WebNotification> webNotifications = await _dataService.GetUsersWebNotifications(userId);

            return Ok(webNotifications);
        }
    }
}