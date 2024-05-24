using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace KinaUnaProgenyApi.Services
{
    public class AzureNotifications(IConfiguration configuration, IDataService dataService, IUserInfoService userInfoService, IUserAccessService userAccessService)
        : IAzureNotifications
    {
        public NotificationHubClient Hub { get; set; } = NotificationHubClient.CreateClientFromConnectionString(configuration["NotificationHubConnection"],
            "kinaunanotifications");

        public async Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "")
        {

            JObject payload = new(
                new JProperty("data", new JObject(new JProperty("title", title), new JProperty("message", message))),
                new JProperty("notData", timeLineItem.TimeLineId));

            string alert = "{\"aps\":{\"alert\":\"" + message + "\"},\"message\":\"" + message + "\",\"notData\":\"" + timeLineItem.TimeLineId + "\", \"content-available\":1}";

            List<UserAccess> userList = await userAccessService.GetProgenyUserAccessList(timeLineItem.ProgenyId);
            foreach (UserAccess userAcces in userList)
            {
                if (userAcces.AccessLevel <= timeLineItem.AccessLevel)
                {
                    UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userAcces.UserId);
                    if (userInfo != null)
                    {
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

                        _ = await dataService.AddMobileNotification(notification);


                        string userTag = "userEmail:" + userAcces.UserId.ToUpper();
                        _ = await Hub.SendFcmNativeNotificationAsync(payload.ToString(Newtonsoft.Json.Formatting.None), userTag);
                        _ = await Hub.SendAppleNativeNotificationAsync(alert, userTag);
                    }
                }
            }

        }
    }
}
