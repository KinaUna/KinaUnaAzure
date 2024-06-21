using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KinaUnaWeb.Hubs
{
    [AllowAnonymous]
    public class WebNotificationHub(IUserInfosHttpClient userInfosHttpClient, IWebNotificationsService notificationsService) : Hub
    {
        readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

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

        public async Task GetUpdateForUser(int count = 10, int start = 0)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext()?.User.FindFirst("timezone")?.Value ?? Constants.DefaultTimezone;
            if (userId != "NoUser")
            {
                List<WebNotification> notifications = await notificationsService.GetLatestNotifications(userId, 0, 10, true);

                notifications = notifications.OrderByDescending(n => n.DateTime).Skip(start).Take(count).ToList();

                if (notifications.Count != 0)
                {
                    foreach (WebNotification webNotification in notifications)
                    {
                        if (string.IsNullOrEmpty(webNotification.Link))
                        {
                            webNotification.Link = "/Notifications?Id=" + webNotification.Id;
                        }
                        webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(webNotification.DateTime,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Custom format.
                        string sendResult = System.Text.Json.JsonSerializer.Serialize(webNotification, _serializeOptions);
                        await Clients.Caller.SendAsync("ReceiveMessage", sendResult);
                    }
                }
            }
        }

        public async Task SendUpdateToUser(WebNotification notification)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            UserInfo currentUserInfo = await userInfosHttpClient.GetUserInfoByUserId(userId);
            
            // Todo: Check if sender has access rights to send to receiver.

            if (userId != "NoUser")
            {
                UserInfo userinfo;
                if (notification.To.Contains('@'))
                {
                    userinfo = await userInfosHttpClient.GetUserInfo(notification.To);
                    notification.To = userinfo.UserId;
                }
                else
                {
                    userinfo = await userInfosHttpClient.GetUserInfoByUserId(notification.To);
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

                notification = await notificationsService.SaveNotification(notification);

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
                await Clients.Caller.SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(webNotification, _serializeOptions));
            }

        }

        public async Task SetRead(string notification)
        {
            string userId = Context.GetHttpContext()?.User.FindFirst("sub")?.Value ?? "NoUser";
            bool idParsed = int.TryParse(notification, out int id);
            if (idParsed)
            {
                WebNotification updateNotification = await notificationsService.GetNotificationById(id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (string.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = true;

                        updateNotification = await notificationsService.UpdateNotification(updateNotification);

                        await Clients.User(userId).SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(updateNotification, _serializeOptions));
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
                WebNotification updateNotification = await notificationsService.GetNotificationById(id);

                if (updateNotification != null)
                {
                    if (userId == updateNotification.To)
                    {
                        if (string.IsNullOrEmpty(updateNotification.Link))
                        {
                            updateNotification.Link = "/Notifications?Id=" + updateNotification.Id;
                        }
                        updateNotification.IsRead = false;

                        updateNotification = await notificationsService.UpdateNotification(updateNotification);

                        await Clients.User(userId).SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(updateNotification, _serializeOptions));
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
                WebNotification deleteNotification = await notificationsService.GetNotificationById(id);
                
                if (deleteNotification != null)
                {
                    if (userId == deleteNotification.To)
                    {
                        await notificationsService.RemoveNotification(deleteNotification);

                        await Clients.User(userId).SendAsync("DeleteMessage", System.Text.Json.JsonSerializer.Serialize(deleteNotification, _serializeOptions));
                    }
                }
            }
        }

        public async Task SendAdminUpdateToUser(WebNotification notification)
        {
            string userEmail = Context.GetHttpContext()?.User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = Context.GetHttpContext()?.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userEmail);

            if (userInfo.IsKinaUnaAdmin)
            {
                if (notification.To == "OnlineUsers")
                {
                    await Clients.All.SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(notification, _serializeOptions));
                }
                else
                {
                    notification.DateTime = DateTime.UtcNow;

                    notification = await notificationsService.SaveNotification(notification);
                    
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
                    await Clients.Caller.SendAsync("ReceiveMessage", System.Text.Json.JsonSerializer.Serialize(webNotification, _serializeOptions));
                }
            }
        }
    }
}
