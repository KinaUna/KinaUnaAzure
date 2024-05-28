using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController(
        IHubContext<WebNotificationHub> hubContext,
        ImageStore imageStore,
        IWebNotificationsService webNotificationsService,
        IViewModelSetupService viewModelSetupService)
        : Controller
    {
        public async Task<IActionResult> Index(int Id = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NotificationsListViewModel model = new(baseModel);
            
            model.NotificationsList = await webNotificationsService.GetUsersNotifications(model.CurrentUser.UserId);
            
            if (model.NotificationsList.Count != 0)
            {
                model.NotificationsList = [.. model.NotificationsList.OrderBy(n => n.DateTime)];
                model.NotificationsList.Reverse();
                foreach (WebNotification notif in model.NotificationsList)
                {
                    notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                    if (!notif.Icon.StartsWith('/'))
                    {
                        notif.Icon = imageStore.UriFor(notif.Icon, "profiles");
                    }
                }
            }

            if (Id == 0) return View(model);

            WebNotification notification = await webNotificationsService.GetNotificationById(Id);
            if (notification == null || notification.To != model.CurrentUser.UserId) return View(model);

            notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notification.DateTime,
                TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
            model.SelectedNotification = notification;
            if (!notification.Icon.StartsWith('/'))
            {
                notification.Icon = imageStore.UriFor(notification.Icon, "profiles");
            }

            return View(model);
        }

        public async Task<IActionResult> ShowNotification(WebNotification notification)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            WebNotificationViewModel model = new(baseModel);
            
            if (!notification.Icon.StartsWith('/'))
            {
                notification.Icon = imageStore.UriFor(notification.Icon, "profiles");
            }

            if (model.CurrentUser.UserId == notification.To)
            {
                model.WebNotification = notification;
            }

            return PartialView(model);
        }

        public async Task<IActionResult> ShowUpdatedNotification(WebNotification notification)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            WebNotificationViewModel model = new(baseModel);
            
            if (!notification.Icon.StartsWith('/'))
            {
                notification.Icon = imageStore.UriFor(notification.Icon, "profiles");
            }

            if (model.CurrentUser.UserId == notification.To)
            {
                model.WebNotification = notification;
            }

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

            await hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));

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

            await hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));

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