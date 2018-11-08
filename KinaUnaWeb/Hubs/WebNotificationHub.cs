using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KinaUnaWeb.Hubs
{
    [AllowAnonymous]
    public class WebNotificationHub: Hub
    {
        private readonly WebDbContext _context;

        public WebNotificationHub(WebDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";

            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, "Online");
            
            await base.OnConnectedAsync();

            await GetUpdateForUser();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, "Online");
            await base.OnDisconnectedAsync(exception);
        }
        public async Task GetUpdateForUser()
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone").Value;
            if (userId != "NoUser")
            {
                List<WebNotification> notifications = await
                    _context.WebNotificationsDb.Where(w => w.IsRead == false && w.To == userId).ToListAsync();
                if (notifications.Any())
                {
                    foreach (WebNotification webn in notifications)
                    {
                        webn.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webn.DateTime,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        webn.DateTimeString = webn.DateTime.ToString("dd-MMM-yyyy HH:mm");
                        string sendResult = JsonConvert.SerializeObject(webn);
                        await Clients.Caller.SendAsync("ReceiveMessage", sendResult);
                    }
                }
            }
        }

        public async Task SendUpdateToUser(WebNotification notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            
            if (userId != "NoUser")
            {
                notification.From = userId;
                await _context.WebNotificationsDb.AddAsync(notification);
                await _context.SaveChangesAsync();

                WebNotification webNotification = new WebNotification();
                webNotification.Title = "Notification Sent to " + notification.To;
                webNotification.Message = "";
                webNotification.From = "KinaUna.com";
                webNotification.Type = "Notification";
                webNotification.DateTime = DateTime.UtcNow;
                await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }

        }


        public async Task TestHello()
        {
            string userEmail = Context.GetHttpContext().User.FindFirst("email").Value;
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone").Value;
            if (userEmail.ToUpper() == "PER.MOGENSEN@GMAIL.COM")
            {
                WebNotification webNotification = new WebNotification();
                webNotification.Title = "Greeting";
                webNotification.Message = "Hello!";
                webNotification.From = "KinaUna.com";
                webNotification.Type = "Notification";
                webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }
        }
    }
}
