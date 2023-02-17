using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
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
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IWebNotificationsService _webNotificationsService;

        public NotificationsController(IHubContext<WebNotificationHub> hubContext, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, IWebNotificationsService webNotificationsService)
        {
            _hubContext = hubContext;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _webNotificationsService = webNotificationsService;
        }

        public async Task<IActionResult> Index(int Id = 0)
        {
            NotificationsListViewModel model = new NotificationsListViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

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
            WebNotificationViewModel model = new WebNotificationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

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
            WebNotificationViewModel model = new WebNotificationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

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
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
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
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
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
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
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