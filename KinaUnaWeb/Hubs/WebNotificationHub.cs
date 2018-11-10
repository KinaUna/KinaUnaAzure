using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
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
        private readonly IProgenyHttpClient _progenyHttpClient;

        public WebNotificationHub(WebDbContext context, IProgenyHttpClient progenyHttpClient )
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";

            var connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, "Online");
            await Groups.AddToGroupAsync(connectionId, userId);
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("UserInfo", Context.UserIdentifier);
            await GetUpdateForUser();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, "Online");
            await Groups.RemoveFromGroupAsync(connectionId, userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task GetUpdateForUser(int count = 10, int start = 1)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone").Value;
            if (userId != "NoUser")
            {
                List<WebNotification> notifications = await
                    _context.WebNotificationsDb.Where(w => w.To == userId).ToListAsync();
                if (notifications.Any())
                {
                    int indexer = 0;
                    notifications = notifications.OrderBy(n => n.DateTime).Reverse().ToList();
                    foreach (WebNotification webn in notifications)
                    {

                        if (count == 0 || (indexer >= start -1 && indexer < count + start -1) )
                        {
                            webn.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webn.DateTime,
                                TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                            webn.DateTimeString = webn.DateTime.ToString("dd-MMM-yyyy HH:mm");
                            string sendResult = JsonConvert.SerializeObject(webn);
                            await Clients.Caller.SendAsync("ReceiveMessage", sendResult);
                        }

                        indexer++;
                        if (count != 0)
                        {
                            count--;
                        }
                    }
                }
            }
        }

        public async Task SendUpdateToUser(WebNotification notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            string userEmail = Context.GetHttpContext().User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            // Todo: Check if sender has access rights to send to receiver.

            if (userId != "NoUser")
            {
                UserInfo userinfo = new UserInfo();
                if (notification.To.Contains('@'))
                {
                    userinfo = await _progenyHttpClient.GetUserInfo(notification.To);
                    notification.To = userinfo.UserId;
                }
                else
                {
                    userinfo = await _progenyHttpClient.GetUserInfoByUserId(notification.To);
                }


                notification.From = userId;
                if (!String.IsNullOrEmpty(userinfo.ProfilePicture))
                {
                    notification.Icon = userinfo.ProfilePicture;
                }
                else
                {
                    notification.Icon = "/photodb/profile.jpg";
                }

                notification.DateTime = DateTime.UtcNow;
                await _context.WebNotificationsDb.AddAsync(notification);
                await _context.SaveChangesAsync();

                WebNotification webNotification = new WebNotification();
                webNotification.Title = "Notification Sent to " + notification.To;
                webNotification.Message = "";
                webNotification.From = "KinaUna.com";
                webNotification.Type = "Notification";
                webNotification.DateTime = DateTime.UtcNow;
                webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webNotification.DateTime,
                    TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                
                await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }

        }

        public async Task SetRead(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id = 0;
            bool idParsed = Int32.TryParse(notification, out id);
            if (idParsed)
            {
                WebNotification updateNotification =
                    await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        updateNotification.IsRead = true;
                        _context.WebNotificationsDb.Update(updateNotification);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task SetUnread(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id = 0;
            bool idParsed = Int32.TryParse(notification, out id);
            if (idParsed)
            {
                WebNotification updateNotification =
                    await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        updateNotification.IsRead = false;
                        _context.WebNotificationsDb.Update(updateNotification);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task DeleteNotification(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id = 0;
            bool idParsed = Int32.TryParse(notification, out id);
            if (idParsed)
            {
                WebNotification deleteNotification =
                    await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

                if (deleteNotification != null)
                {
                    if (userId == deleteNotification.To)
                    {
                        _context.WebNotificationsDb.Remove(deleteNotification);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task SendAdminUpdateToUser(WebNotification notification)
        {
            string userEmail = Context.GetHttpContext().User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (userEmail.ToUpper() == "PER.MOGENSEN@GMAIL.COM")
            {
                if (notification.To == "OnlineUsers")
                {
                    await Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
                }
                else
                {
                    notification.DateTime = DateTime.UtcNow;
                    await _context.WebNotificationsDb.AddAsync(notification);
                    await _context.SaveChangesAsync();

                    WebNotification webNotification = new WebNotification();
                    webNotification.Title = "Notification Sent to " + notification.To;
                    webNotification.Message = "";
                    webNotification.From = "KinaUna.com";
                    webNotification.Type = "Notification";
                    webNotification.DateTime = DateTime.UtcNow;
                    webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webNotification.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
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
