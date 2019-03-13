using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
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

        public async Task GetUpdateForUser(int count = 10, int start = 1)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone").Value;
            if (userId != "NoUser")
            {
                List<WebNotification> notifications = await
                    _context.WebNotificationsDb.Where(w => w.To == userId).OrderByDescending(n => n.DateTime).Skip(start -1).Take(count).ToListAsync();
                if (notifications.Any())
                {
                    foreach (WebNotification webn in notifications)
                    {
                        if (String.IsNullOrEmpty(webn.Link))
                        {
                            webn.Link = "/Notifications?Id=" + webn.Id;
                        }
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
            string userTimeZone = Context.GetHttpContext().User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            // Todo: Check if sender has access rights to send to receiver.

            if (userId != "NoUser")
            {
                UserInfo userinfo;
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
                if (String.IsNullOrEmpty(webNotification.Link))
                {
                    webNotification.Link = "/Notifications?Id=" + webNotification.Id;
                }
                await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }

        }

        public async Task SetRead(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id;
            bool idParsed = Int32.TryParse(notification, out id);
            if (idParsed)
            {
                WebNotification updateNotification =
                    await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (String.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = true;
                        _context.WebNotificationsDb.Update(updateNotification);
                        await _context.SaveChangesAsync();
                        await Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                    }
                }
            }
        }

        public async Task SetUnread(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id;
            bool idParsed = Int32.TryParse(notification, out id);
            if (idParsed)
            {
                WebNotification updateNotification =
                    await _context.WebNotificationsDb.SingleOrDefaultAsync(n => n.Id == id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (String.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = false;
                        _context.WebNotificationsDb.Update(updateNotification);
                        await _context.SaveChangesAsync();
                        await Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                    }
                }
            }
        }

        public async Task DeleteNotification(string notification)
        {
            string userId = Context.GetHttpContext().User.FindFirst("sub")?.Value ?? "NoUser";
            int id;
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
                        await Clients.User(userId).SendAsync("DeleteMessage", JsonConvert.SerializeObject(deleteNotification));
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
                    if (String.IsNullOrEmpty(webNotification.Link))
                    {
                        webNotification.Link = "/Notifications?Id=" + webNotification.Id;
                    }
                    await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
            }
        }
    }
}
