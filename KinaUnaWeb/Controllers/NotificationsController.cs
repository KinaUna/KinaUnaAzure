using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly ImageStore _imageStore;
        private readonly IUserInfosHttpClient _userInfosHttpClient;

        public NotificationsController(WebDbContext context, IHubContext<WebNotificationHub> hubContext, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient)
        {
            _context = context; // Todo: Replace _context with httpClient
            _hubContext = hubContext;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
        }

        public async Task<IActionResult> Index(int Id = 0)
        {
            NotificationsListViewModel model = new NotificationsListViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            model.NotificationsList = await _context.WebNotificationsDb.Where(n => n.To == model.CurrentUser.UserId).ToListAsync();
            
            if (model.NotificationsList.Any())
            {
                model.NotificationsList = model.NotificationsList.OrderBy(n => n.DateTime).ToList();
                model.NotificationsList.Reverse();
                foreach (WebNotification notif in model.NotificationsList)
                {
                    notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                    if (!notif.Icon.StartsWith("/") && !notif.Icon.StartsWith("http"))
                    {
                        notif.Icon = _imageStore.UriFor(notif.Icon, "profiles");
                    }
                }
            }
            if (Id != 0)
            {
                WebNotification notification = await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == Id);
                if (notification != null && notification.To == model.CurrentUser.UserId)
                {
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notification.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    model.SelectedNotification = notification;
                    if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
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

            if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
            }

            if (model.CurrentUser.UserId == notification.To || model.CurrentUser.UserId == notification.From)
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

            if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
            }

            if (model.CurrentUser.UserId == notification.To || model.CurrentUser.UserId == notification.From)
            {
                model.WebNotification = notification;
            }

            return PartialView(model);
        }

        public async Task<IActionResult> SetUnread(int Id)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            WebNotification updateNotification =
                await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    updateNotification.IsRead = false;
                    _context.WebNotificationsDb.Update(updateNotification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }

        public async Task<IActionResult> SetRead(int Id)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            WebNotification updateNotification =
                await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    updateNotification.IsRead = true;
                    _context.WebNotificationsDb.Update(updateNotification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }

        public async Task<IActionResult> Remove(int Id)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            WebNotification updateNotification =
                await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == Id);

            if (updateNotification != null)
            {
                if (userId == updateNotification.To)
                {
                    _context.WebNotificationsDb.Remove(updateNotification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userId).SendAsync("DeleteMessage", JsonConvert.SerializeObject(updateNotification));
                }
            }

            return Ok();
        }
    }
}