using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Implementation of the IAzureNotifications interface, used to manage mobile notifications.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="notificationsService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    public class AzureNotifications(IConfiguration configuration, INotificationsService notificationsService, IUserInfoService userInfoService, IUserAccessService userAccessService)
        : IAzureNotifications
    {
        public NotificationHubClient Hub { get; set; } = NotificationHubClient.CreateClientFromConnectionString(configuration["NotificationHubConnection"],
            "kinaunanotifications");

        /// <summary>
        /// Sends a notification to all users with access to a TimeLineItem.
        /// For use when a TimeLineItem is created or updated.
        /// Also saves the notification in the database for later retrieval.
        /// </summary>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The body/content of the message.</param>
        /// <param name="timeLineItem">The TimeLineItem the notification is for.</param>
        /// <param name="iconLink">Link to the image used as icon when the notification is displayed in the notification history.</param>
        public async Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "")
        {

            JObject payload = new(
                new JProperty("data", new JObject(new JProperty("title", title), new JProperty("message", message))),
                new JProperty("notData", timeLineItem.TimeLineId));

            string alert = "{\"aps\":{\"alert\":\"" + message + "\"},\"message\":\"" + message + "\",\"notData\":\"" + timeLineItem.TimeLineId + "\", \"content-available\":1}";

            List<UserAccess> userList = await userAccessService.GetProgenyUserAccessList(timeLineItem.ProgenyId);
            foreach (UserAccess userAcces in userList)
            {
                if (userAcces.AccessLevel > timeLineItem.AccessLevel) continue;

                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userAcces.UserId);
                if (userInfo == null) continue;

                MobileNotification notification = new()
                {
                    UserId = userInfo.UserId,
                    IconLink = iconLink,
                    ItemId = timeLineItem.ItemId,
                    ItemType = timeLineItem.ItemType,
                    Language = "EN",
                    Message = message,
                    Title = title,
                    Time = DateTime.UtcNow,
                    Read = false
                };

                _ = await notificationsService.AddMobileNotification(notification);


                string userTag = "userEmail:" + userAcces.UserId.ToUpper();
                _ = await Hub.SendFcmNativeNotificationAsync(payload.ToString(Newtonsoft.Json.Formatting.None), userTag);
                _ = await Hub.SendAppleNativeNotificationAsync(alert, userTag);
            }

        }
    }
}
