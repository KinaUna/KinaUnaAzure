using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly WebDbContext _context;

        public NotificationsController(WebDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int Id = 0)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = User.FindFirst("timezone")?.Value ?? "NoUser";
            List<WebNotification> notificationsList = await _context.WebNotificationsDb.Where(n => n.To == userId).ToListAsync();
            notificationsList.Reverse();

            if (notificationsList.Any())
            {
                notificationsList = notificationsList.OrderBy(n => n.DateTime).ToList();
                foreach (WebNotification notif in notificationsList)
                {
                    notif.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notif.DateTimeString = notif.DateTime.ToString("dd-MMM-yyyy HH:mm");
                }
            }
            if (Id != 0)
            {
                WebNotification notification = await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == Id);
                if (notification.To == userId)
                {
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    ViewBag.SelectedNotification = notification;
                }
            }
            
            return View(notificationsList);
        }

        public IActionResult ShowNotification(WebNotification notification)
        {
            return PartialView(notification);
        }
    }
}