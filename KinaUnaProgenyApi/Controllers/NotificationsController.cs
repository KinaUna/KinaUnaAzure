using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for notifications.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="imageStore"></param>
    /// <param name="notificationsService"></param>
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController(IAzureNotifications azureNotifications, IImageStore imageStore, INotificationsService notificationsService) : ControllerBase
    {
        /// <summary>
        /// Sends a push notification to a user, using the Azure Notification Hub.
        /// </summary>
        /// <param name="pns">Platform to send notification to. wms=Windows, apns=iOS, fcm=android</param>
        /// <param name="message">The content of the notification.</param>
        /// <param name="to_tag">The tag with user id.</param>
        /// <returns>HttpStatusCode.Ok if the notification was sent. HttpStatusCode.Unauthorized if the user is not logged in. HttpStatusCode.InternalServerError if something went wrong.</returns>
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

        /// <summary>
        /// Gets the latest mobile notifications for a user.
        /// </summary>
        /// <param name="count">The number of notifications to get.</param>
        /// <param name="start">The number of notifications to skip.</param>
        /// <param name="language">The language set by the current user.</param>
        /// <returns>A list of MobileNotification.</returns>
        [HttpGet]
        [Route("[action]/{count:int}/{start:int}/{language}")]
        public async Task<IActionResult> Latest(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await notificationsService.GetUsersMobileNotifications(userId, language);
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

        /// <summary>
        /// Gets the latest unread mobile notifications for a user.
        /// </summary>
        /// <param name="count">The number of notifications to get.</param>
        /// <param name="start">The number of notification to skip.</param>
        /// <param name="language">The user's current selected language.</param>
        /// <returns>List of MobileNotification.</returns>
        [HttpGet]
        [Route("[action]/{count:int}/{start:int}/{language}")]
        public async Task<IActionResult> Unread(int count, int start = 0, string language = "EN")
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            List<MobileNotification> notifications = await notificationsService.GetUsersMobileNotifications(userId, language);
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

        /// <summary>
        /// Updates a mobile notification.
        /// </summary>
        /// <param name="id">The NotificationId of the MobileNotification to update.</param>
        /// <param name="value">The MobileNotification object with the updated properties.</param>
        /// <returns>The updated MobileNotification object.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] MobileNotification value)
        {
            string userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            MobileNotification mobileNotification = await notificationsService.GetMobileNotification(id);
            if (mobileNotification == null || mobileNotification.UserId != userId) return Ok(value);
            
            mobileNotification.Read = value.Read;
            mobileNotification = await notificationsService.UpdateMobileNotification(mobileNotification);

            return Ok(mobileNotification);

        }

        /// <summary>
        /// Deletes a mobile notification.
        /// </summary>
        /// <param name="id">The NotificationId of the entity to delete.</param>
        /// <returns>NoContent if successful. Unauthorized if the user is not allow to delete the entity. NotFound if the entity doesn't exist.</returns>
        // DELETE api/notifications/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            MobileNotification mobileNotification = await notificationsService.GetMobileNotification(id);
            if (mobileNotification != null)
            {
                if (mobileNotification.UserId == User.GetUserId())
                {
                    _ = await notificationsService.DeleteMobileNotification(mobileNotification);
                }
                else
                {
                    return Unauthorized();
                }

                return NoContent();
            }

            return NotFound();
        }

        /// <summary>
        /// Adds a push device to the database.
        /// </summary>
        /// <param name="device">The PushDevices entity to add.</param>
        /// <returns>The added PushDevice object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddPushDevice([FromBody] PushDevices device)
        {
            //Todo: Add UserId to PushDevice and check if user should have access.

            device = await notificationsService.AddPushDevice(device);

            return Ok(device);
        }

        /// <summary>
        /// Removes a push device from the database.
        /// </summary>
        /// <param name="device">The PushDevices entity to remove.</param>
        /// <returns>The deleted PushDevices object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> RemovePushDevice([FromBody] PushDevices device)
        {
            await notificationsService.RemovePushDevice(device);

            return Ok(device);
        }

        /// <summary>
        /// Gets a push device by id.
        /// </summary>
        /// <param name="id">The id of the PushDevices entity.</param>
        /// <returns>The PushDevices object with the provided id.</returns>
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> GetPushDeviceById(int id)
        {
            PushDevices device = await notificationsService.GetPushDeviceById(id);

            return Ok(device);
        }

        /// <summary>
        /// Returns a list of push devices for a user.
        /// </summary>
        /// <param name="userId">The user id to retrieve push devices for.</param>
        /// <returns>A list of UserDevices associated with the user.</returns>
        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetPushDevicesListByUserId(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<PushDevices> devices = await notificationsService.GetPushDevicesListByUserId(userId);

            return Ok(devices);
        }

        /// <summary>
        /// Gets a push device where all the PushDevice properties match.
        /// </summary>
        /// <param name="device">PushDevices object with the properties to retrieve.</param>
        /// <returns>The PushDevice with matching properties.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetPushDevice([FromBody] PushDevices device)
        {
            device = await notificationsService.GetPushDevice(device);

            return Ok(device);
        }

        /// <summary>
        /// Adds a WebNotification entity to the database.
        /// </summary>
        /// <param name="notification">The WebNotification object to add.</param>
        /// <returns>The added WebNotification object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (notification.To != currentUserId)
            {
                return Unauthorized();
            }

            notification = await notificationsService.AddWebNotification(notification);

            return Ok(notification);
        }

        /// <summary>
        /// Updates a WebNotification entity in the database.
        /// </summary>
        /// <param name="notification">The WebNotification object with the updated properties.</param>
        /// <returns>The updated WebNotification object, or Unauthorized if the user isn't allow to access it.</returns>
        [HttpPut]
        [Route("[action]")]
        public async Task<IActionResult> UpdateWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            WebNotification existingNotification = await notificationsService.GetWebNotificationById(notification.Id);
            if (existingNotification.To != currentUserId || notification.To != currentUserId)
            {
                return Unauthorized();
            }

            notification = await notificationsService.UpdateWebNotification(notification);

            return Ok(notification);
        }

        /// <summary>
        /// Deletes a WebNotification entity from the database.
        /// </summary>
        /// <param name="notification">The WebNotification to delete.</param>
        /// <returns>The deleted WebNotification.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> RemoveWebNotification([FromBody] WebNotification notification)
        {
            string currentUserId = User.GetUserId() ?? "";
            WebNotification existingNotification = await notificationsService.GetWebNotificationById(notification.Id);
            if (existingNotification.To != currentUserId || notification.To != currentUserId)
            {
                return Unauthorized();
            }

            await notificationsService.RemoveWebNotification(notification);

            return Ok(notification);
        }

        /// <summary>
        /// Gets a WebNotification by id.
        /// </summary>
        /// <param name="id">The id of the WebNotification to retrieve.</param>
        /// <returns>The WebNotification with the provided id. Unauthorized if the user doesn't have access.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> GetWebNotificationById(int id)
        {
            WebNotification webNotification = await notificationsService.GetWebNotificationById(id);

            string currentUserId = User.GetUserId() ?? "";
            if (webNotification.To != currentUserId)
            {
                return Unauthorized();
            }

            return Ok(webNotification);
        }

        /// <summary>
        /// Gets a list of WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The user id of the user to retrieve WebNotifications for.</param>
        /// <returns>A list of WebNotifications.</returns>
        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetUsersNotifications(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<WebNotification> webNotifications = await notificationsService.GetUsersWebNotifications(userId);

            return Ok(webNotifications);
        }

        /// <summary>
        /// Gets the latest WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <param name="start">The number of WebNotifications to skip.</param>
        /// <param name="count">The number of WebNotification to retrieve.</param>
        /// <param name="unreadOnly">Filter by unread, if True all read WebNotifications will be removed.</param>
        /// <returns>List of WebNotification sorted by date, newest first.</returns>
        [HttpGet]
        [Route("[action]/{userId}/{start:int}/{count:int}/{unreadOnly:bool}")]
        public async Task<IActionResult> GetLatestWebNotifications(string userId, int start, int count, bool unreadOnly)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            List<WebNotification> notificationsList = await notificationsService.GetLatestWebNotifications(userId, start, count, unreadOnly);

            return Ok(notificationsList);
        }

        /// <summary>
        /// Gets the number of WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user to count WebNotifications for.</param>
        /// <returns>An int with the number of WebNotifications for the user.</returns>
        [HttpGet]
        [Route("[action]/{userId}")]
        public async Task<IActionResult> GetUsersNotificationsCount(string userId)
        {
            string currentUserId = User.GetUserId() ?? "";
            if (userId != currentUserId)
            {
                return Unauthorized();
            }

            int notificationsCount = await notificationsService.GetUsersNotificationsCount(userId);

            return Ok(notificationsCount);
        }
    }
}