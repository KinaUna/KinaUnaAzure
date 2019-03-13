using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
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

        public NotificationsController(WebDbContext context, IHubContext<WebNotificationHub> hubContext, ImageStore imageStore)
        {
            _context = context; // Todo: Replace _context with httpClient
            _hubContext = hubContext;
            _imageStore = imageStore;
        }

        public async Task<IActionResult> Index(int Id = 0)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = User.FindFirst("timezone")?.Value ?? "NoUser";
            List<WebNotification> notificationsList = await _context.WebNotificationsDb.Where(n => n.To == userId).ToListAsync();
            
            if (notificationsList.Any())
            {
                notificationsList = notificationsList.OrderBy(n => n.DateTime).ToList();
                notificationsList.Reverse();
                foreach (WebNotification notif in notificationsList)
                {
                    notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notif.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
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
                if (notification.To == userId)
                {
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(notification.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    ViewBag.SelectedNotification = notification;
                    if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
                    {
                        notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
                    }
                }
            }
            
            return View(notificationsList);
        }

        public IActionResult ShowNotification(WebNotification notification)
        {
            if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
            }
            return PartialView(notification);
        }

        public IActionResult ShowUpdatedNotification(WebNotification notification)
        {
            if (!notification.Icon.StartsWith("/") && !notification.Icon.StartsWith("http"))
            {
                notification.Icon = _imageStore.UriFor(notification.Icon, "profiles");
            }
            return PartialView(notification);
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