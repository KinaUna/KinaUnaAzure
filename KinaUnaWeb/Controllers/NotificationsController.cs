using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.Notifications;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController(
        IHubContext<WebNotificationHub> hubContext,
        IWebNotificationsService webNotificationsService,
        IViewModelSetupService viewModelSetupService,
        IUserInfosHttpClient userInfosHttpClient)
        : Controller
    {
        readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        /// <summary>
        /// Notification index page. Shows a list of notifications.
        /// </summary>
        /// <param name="Id">Optional Notification Id to highlight.</param>
        /// <param name="count">Number of Notfications to load.</param>
        /// <returns>View with NotificationsListViewModel.</returns>
        public async Task<IActionResult> Index(int Id = 0, int count = 10)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NotificationsListViewModel model = new(baseModel)
            {
                Count = count
            };

            if (Id == 0) return View(model);

            WebNotification notification = await webNotificationsService.GetNotificationById(Id);
            if (notification == null || notification.To != model.CurrentUser.UserId) return View(model);

            notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notification.DateTime,
                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
            model.SelectedNotification = notification;

            notification.Icon = notification.GetIconUrl();
            
            return View(model);
        }

        /// <summary>
        /// HttpPost method to get Json of a list of notifications.
        /// </summary>
        /// <param name="parameters">WebNotificationsParameters to specify which Notes to include.</param>
        /// <returns>Json of WebNotificationsList.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetWebNotificationsPage([FromBody] WebNotificationsParameters parameters)
        {
            string userId = User.GetUserId() ?? Constants.DefaultUserId;

            WebNotificationsList webNotificationsList = new()
            {
                NotificationsList = await webNotificationsService.GetLatestNotifications(userId, parameters.Skip, parameters.Count, false),
                AllNotificationsCount = await webNotificationsService.GetUsersNotificationsCount(userId)
            };

            webNotificationsList.RemainingItemsCount = webNotificationsList.AllNotificationsCount - parameters.Skip - webNotificationsList.NotificationsList.Count;

            if (webNotificationsList.NotificationsList.Count == 0) return Json(webNotificationsList);

            if (parameters.unreadOnly)
            {
                webNotificationsList.NotificationsList = [.. webNotificationsList.NotificationsList.Where(n => !n.IsRead)];
            }

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(userId);

            string timezoneId = userInfo.Timezone;

            foreach (WebNotification notif in webNotificationsList.NotificationsList)
            {
                notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                    TimeZoneInfo.FindSystemTimeZoneById(timezoneId));
                notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                notif.Icon = notif.GetIconUrl();
            }
            return Json(webNotificationsList);
        }

        /// <summary>
        /// HttpPost method to get a single notification as a PartialView.
        /// </summary>
        /// <param name="model">WebNotificationViewModel with parameters for displaying a Note.</param>
        /// <returns>PartialView with WebNotificationViewModel.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetWebNotificationElement([FromBody] WebNotificationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);

            model.WebNotification = await webNotificationsService.GetNotificationById(model.Id);
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(model.WebNotification.To);
            model.WebNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(model.WebNotification.DateTime, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
            model.WebNotification.DateTimeString = model.WebNotification.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value

            model.WebNotification.Icon = model.WebNotification.GetIconUrl();

            return PartialView("_GetWebNotificationElementPartial", model);
        }

        /// <summary>
        /// Partial view to show a single notification in menus and lists.
        /// </summary>
        /// <param name="notification">WebNotification object to show.</param>
        /// <returns>PartialView with WebNotificationViewModel.</returns>
        public async Task<IActionResult> ShowNotification(WebNotification notification)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            WebNotificationViewModel model = new(baseModel);
            
            if (model.CurrentUser.UserId == notification.To)
            {
                model.WebNotification = await webNotificationsService.GetNotificationById(notification.Id);
                model.Id = notification.Id;
            }

            model.WebNotification.Icon = model.WebNotification.GetIconUrl();
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(model.WebNotification.To);
            model.WebNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(model.WebNotification.DateTime, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
            model.WebNotification.DateTimeString = model.WebNotification.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value

            return PartialView(model);
        }


        /// <summary>
        /// Set a WebNotification as unread.
        /// Uses SignalR to send the updated notification to the user.
        /// </summary>
        /// <param name="Id">The Id of the WebNotification to update.</param>
        /// <returns>OkObjectResult with string.</returns>
        public async Task<IActionResult> SetUnread(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return NotFound();

            if (userId != updateNotification.To) return NotFound();

            updateNotification.IsRead = false;
            updateNotification = await webNotificationsService.UpdateNotification(updateNotification);

            await hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonSerializer.Serialize(updateNotification, _serializeOptions));

            return Ok("Notification set as unread. Id: " + Id);
        }

        /// <summary>
        /// Set a WebNotification as read.
        /// Uses SignalR to send the updated notification to the user.
        /// </summary>
        /// <param name="Id">The Id of the WebNotification to update.</param>
        /// <returns>OkObjectResult with string.</returns>
        public async Task<IActionResult> SetRead(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return NotFound();

            if (userId != updateNotification.To) return NotFound();

            updateNotification.IsRead = true;
                    
            updateNotification = await webNotificationsService.UpdateNotification(updateNotification);

            await hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonSerializer.Serialize(updateNotification, _serializeOptions));

            return Ok("Notification set as read. Id: " + Id);
        }

        /// <summary>
        /// Set all unread WebNotifications as read.
        /// </summary>
        /// <returns>OkObjectResult with string.</returns>
        public async Task<IActionResult> SetAllRead()
        {
            string userId = User.GetUserId() ?? "NoUser";

            List<WebNotification> unreadWebNotifications = await webNotificationsService.GetUsersNotifications(userId);
            unreadWebNotifications = [.. unreadWebNotifications.Where(n => !n.IsRead)];

            foreach (WebNotification notification in unreadWebNotifications)
            {
                WebNotification updateNotification = await webNotificationsService.GetNotificationById(notification.Id);

                if (updateNotification == null || updateNotification.Id == 0) continue;

                if (userId != updateNotification.To) continue;

                updateNotification.IsRead = true;

                _ = await webNotificationsService.UpdateNotification(updateNotification);
                
            }

            //_ = Task.Run(() => Task.FromResult(BatchSetWebNotificationsToRead(unreadWebNotifications))); // Todo: Replace with a more robust solution.
            await hubContext.Clients.User(userId).SendAsync("MarkAllReadMessage", JsonSerializer.Serialize(userId, _serializeOptions));

            return Ok("All notification set as read");
        }

        /// <summary>
        /// Deletes a WebNotification.
        /// </summary>
        /// <param name="Id">The Id of the WebNotification to delete.</param>
        /// <returns>OkObjectResult with string.</returns>
        public async Task<IActionResult> Remove(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return NotFound();

            if (userId != updateNotification.To) return NotFound();

            await webNotificationsService.RemoveNotification(updateNotification);
            await hubContext.Clients.User(userId).SendAsync("DeleteMessage", JsonSerializer.Serialize(updateNotification, _serializeOptions));

            return Ok("Notification removed. Id: " + Id);
        }
    }
}