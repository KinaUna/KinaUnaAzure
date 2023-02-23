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
    public class NotificationsController : Controller
    {
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly ImageStore _imageStore;
        private readonly IWebNotificationsService _webNotificationsService;
        private readonly IViewModelSetupService _viewModelSetupService;

        public NotificationsController(IHubContext<WebNotificationHub> hubContext, ImageStore imageStore,
            IWebNotificationsService webNotificationsService, IViewModelSetupService viewModelSetupService)
        {
            _hubContext = hubContext;
            _imageStore = imageStore;
            _webNotificationsService = webNotificationsService;
            _viewModelSetupService = viewModelSetupService;
        }

        public async Task<IActionResult> Index(int Id = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NotificationsListViewModel model = new NotificationsListViewModel(baseModel);
            
            model.NotificationsList = await _webNotificationsService.GetUsersNotifications(model.CurrentUser.UserId);
            
            if (model.NotificationsList.Any())
            {
                model.NotificationsList = model.NotificationsList.OrderBy(n => n.DateTime).ToList();
                model.NotificationsList.Reverse();
                foreach (WebNotification notif in model.NotificationsList)
                {
                    notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                    if (!notif.Icon.StartsWith("/"))
                    {
                        notif.Icon = _imageStore.UriFor(notif.Icon, "profiles");
                    }
                }
            }

            if (Id != 0)
            {
                WebNotification notification = await _webNotificationsService.GetNotificationById(Id);
                if (notification != null && notification.To == model.CurrentUser.UserId)
                {
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notification.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    model.SelectedNotification = notification;
                    if (!notification.Icon.StartsWith("/"))
                    {
                        notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
                    }
                }
            }
            
            return View(model);
        }

        public async Task<IActionResult> ShowNotification(WebNotification notification)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            WebNotificationViewModel model = new WebNotificationViewModel(baseModel);
            
            if (!notification.Icon.StartsWith("/"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
            }

            if (model.CurrentUser.UserId == notification.To)
            {
                model.WebNotification = notification;
            }

            return PartialView(model);
        }

        public async Task<IActionResult> ShowUpdatedNotification(WebNotification notification)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            WebNotificationViewModel model = new WebNotificationViewModel(baseModel);
            
            if (!notification.Icon.StartsWith("/"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
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
            WebNotification updateNotification = await _webNotificationsService.GetNotificationById(Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    updateNotification.IsRead = false;
                    updateNotification = await _webNotificationsService.UpdateNotification(updateNotification);

                    await _hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }

        public async Task<IActionResult> SetRead(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await _webNotificationsService.GetNotificationById(Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    updateNotification.IsRead = true;
                    
                    updateNotification = await _webNotificationsService.UpdateNotification(updateNotification);

                    await _hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }

        public async Task<IActionResult> Remove(int Id)
        {
            string userId = User.GetUserId() ?? "NoUser";
            WebNotification updateNotification = await _webNotificationsService.GetNotificationById(Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    await _webNotificationsService.RemoveNotification(updateNotification);
                    await _hubContext.Clients.User(userId).SendAsync("DeleteMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }
    }
}