using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KinaUnaWeb.Hubs
{
    [AllowAnonymous]
    public class WebNotificationHub: Hub
    {
        private readonly IWebNotificationsService _notificationsService;
        private readonly IUserInfosHttpClient _userInfosHttpClient;

        public WebNotificationHub(IUserInfosHttpClient userInfosHttpClient, IWebNotificationsService notificationsService)
        {
            _userInfosHttpClient = userInfosHttpClient;
            _notificationsService = notificationsService;
        }

        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(connectionId, "Online");
            await base.OnConnectedAsync();
            await GetUpdateForUser();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, "Online");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task GetUpdateForUser(int count = 10, int start = 1)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext()?.User.FindFirst("timezone")?.Value ?? Constants.DefaultTimezone;
            if (userId != "NoUser")
            {
                List<WebNotification> notifications = await _notificationsService.GetUsersNotifications(userId);

                notifications = notifications.OrderByDescending(n => n.DateTime).Skip(start - 1).Take(count).ToList();

                if (notifications.Any())
                {
                    foreach (WebNotification webn in notifications)
                    {
                        if (string.IsNullOrEmpty(webn.Link))
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
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            UserInfo currentUserInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId);
            
            // Todo: Check if sender has access rights to send to receiver.

            if (userId != "NoUser")
            {
                UserInfo userinfo;
                if (notification.To.Contains('@'))
                {
                    userinfo = await _userInfosHttpClient.GetUserInfo(notification.To);
                    notification.To = userinfo.UserId;
                }
                else
                {
                    userinfo = await _userInfosHttpClient.GetUserInfoByUserId(notification.To);
                }


                notification.From = currentUserInfo.FullName();

                if (!string.IsNullOrEmpty(userinfo.ProfilePicture))
                {
                    notification.Icon = userinfo.ProfilePicture;
                }
                else
                {
                    notification.Icon = "/photodb/profile.jpg";
                }

                notification.DateTime = DateTime.UtcNow;

                notification = await _notificationsService.SaveNotification(notification);

                WebNotification webNotification = new()
                {
                    Title = "Notification Sent to " + notification.To,
                    Message = "",
                    From = "KinaUna.com",
                    Type = "Notification",
                    DateTime = DateTime.UtcNow
                };
                webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webNotification.DateTime,
                    TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
                webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                if (string.IsNullOrEmpty(webNotification.Link))
                {
                    webNotification.Link = "/Notifications?Id=" + webNotification.Id;
                }
                await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }

        }

        public async Task SetRead(string notification)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            bool idParsed = int.TryParse(notification, out int id);
            if (idParsed)
            {
                WebNotification updateNotification = await _notificationsService.GetNotificationById(id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (string.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = true;

                        updateNotification = await _notificationsService.UpdateNotification(updateNotification);

                        await Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                    }
                }
            }
        }

        public async Task SetUnread(string notification)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            bool idParsed = int.TryParse(notification, out int id);
            if (idParsed)
            {
                WebNotification updateNotification = await _notificationsService.GetNotificationById(id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (string.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = false;

                        updateNotification = await _notificationsService.UpdateNotification(updateNotification);

                        await Clients.User(userId).SendAsync("UpdateMessage", JsonConvert.SerializeObject(updateNotification));
                    }
                }
            }
        }

        public async Task DeleteNotification(string notification)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            bool idParsed = int.TryParse(notification, out int id);
            if (idParsed)
            {
                WebNotification deleteNotification = await _notificationsService.GetNotificationById(id);
                
                if (deleteNotification != null)
                {
                    if (userId == deleteNotification.To)
                    {
                        await _notificationsService.RemoveNotification(deleteNotification);

                        await Clients.User(userId).SendAsync("DeleteMessage", JsonConvert.SerializeObject(deleteNotification));
                    }
                }
            }
        }

        public async Task SendAdminUpdateToUser(WebNotification notification)
        {
            string userEmail = Context.GetHttpContext()?.User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext()?.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (userEmail.ToUpper() == "PER.MOGENSEN@GMAIL.COM")
            {
                if (notification.To == "OnlineUsers")
                {
                    await Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
                }
                else
                {
                    notification.DateTime = DateTime.UtcNow;

                    notification = await _notificationsService.SaveNotification(notification);
                    
                    WebNotification webNotification = new()
                    {
                        Title = "Notification Sent to " + notification.To,
                        Message = "",
                        From = "KinaUna.com",
                        Type = "Notification",
                        DateTime = DateTime.UtcNow
                    };
                    webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webNotification.DateTime,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    if (string.IsNullOrEmpty(webNotification.Link))
                    {
                        webNotification.Link = "/Notifications?Id=" + webNotification.Id;
                    }
                    await Clients.Caller.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
            }
        }
    }
}
