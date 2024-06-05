using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.Notifications;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController(
        IHubContext<WebNotificationHub> hubContext,
        IWebNotificationsService webNotificationsService,
        IViewModelSetupService viewModelSetupService)
        : Controller
    {
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetWebNotificationsPage([FromBody] WebNotificationsParameters parameters)
        {
            string userId = User.GetUserId() ?? Constants.DefaultUserId;

            WebNotificationsList webNotificationsList = new()
            {
                NotificationsList = await webNotificationsService.GetLatestNotifications(userId, parameters.Skip, parameters.Count, false)
            };

            webNotificationsList.AllNotificationsCount = await webNotificationsService.GetUsersNotificationsCount(userId);
            webNotificationsList.RemainingItemsCount = webNotificationsList.AllNotificationsCount - parameters.Skip - webNotificationsList.NotificationsList.Count;

            if (webNotificationsList.NotificationsList.Count == 0) return Json(webNotificationsList);

            foreach (WebNotification notif in webNotificationsList.NotificationsList)
            {
                notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                    TimeZoneInfo.FindSystemTimeZoneById(User.GetUserTimeZone()));
                notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                notif.Icon = notif.GetIconUrl();
            }
            return Json(webNotificationsList);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetWebNotificationElement([FromBody] WebNotificationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            model.SetBaseProperties(baseModel);

            model.WebNotification = await webNotificationsService.GetNotificationById(model.Id);

            model.WebNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(model.WebNotification.DateTime, TimeZoneInfo.FindSystemTimeZoneById(User.GetUserTimeZone()));
            model.WebNotification.DateTimeString = model.WebNotification.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value

            model.WebNotification.Icon = model.WebNotification.GetIconUrl();

            return PartialView("_GetWebNotificationElementPartial", model);
        }


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

            model.WebNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(model.WebNotification.DateTime, TimeZoneInfo.FindSystemTimeZoneById(User.GetUserTimeZone()));
            model.WebNotification.DateTimeString = model.WebNotification.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value

            return PartialView(model);
        }


        public async Task<IActionResult> SetUnread(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return Ok();

            if (userId != updateNotification.To) return Ok();

            updateNotification.IsRead = false;
            updateNotification = await webNotificationsService.UpdateNotification(updateNotification);

            await hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(updateNotification));

            return Ok();
        }

        public async Task<IActionResult> SetRead(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return Ok();

            if (userId != updateNotification.To) return Ok();

            updateNotification.IsRead = true;
                    
            updateNotification = await webNotificationsService.UpdateNotification(updateNotification);

            await hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(updateNotification));

            return Ok();
        }

        public async Task<IActionResult> SetAllRead()
        {
            string userId = User.GetUserId() ?? "NoUser";

            List<WebNotification> unreadWebNotifications = await webNotificationsService.GetUsersNotifications(userId);
            unreadWebNotifications = unreadWebNotifications.Where(n => !n.IsRead).ToList();

            foreach (WebNotification notification in unreadWebNotifications)
            {
                WebNotification updateNotification = await webNotificationsService.GetNotificationById(notification.Id);

                if (updateNotification == null || updateNotification.Id == 0) continue;

                if (userId != updateNotification.To) continue;

                updateNotification.IsRead = true;

                _ = await webNotificationsService.UpdateNotification(updateNotification);
                
            }

            //_ = Task.Run(() => Task.FromResult(BatchSetWebNotificationsToRead(unreadWebNotifications))); // Todo: Replace with a more robust solution.
            await hubContext.Clients.User(userId).SendAsync("MarkAllReadMessage", JsonConvert.SerializeObject(userId));

            return Ok();
        }


        public async Task<IActionResult> Remove(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await webNotificationsService.GetNotificationById(Id);

            if (updateNotification == null) return Ok();

            if (userId != updateNotification.To) return Ok();

            await webNotificationsService.RemoveNotification(updateNotification);
            await hubContext.Clients.User(userId).SendAsync("DeleteMessage", JsonConvert.SerializeObject(updateNotification));

            return Ok();
        }
    }
}