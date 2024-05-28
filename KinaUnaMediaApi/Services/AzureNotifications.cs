using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace KinaUnaMediaApi.Services
{
    public class AzureNotifications(IConfiguration configuration, IDataService dataService)
    {
        private NotificationHubClient Hub { get; set; } = NotificationHubClient.CreateClientFromConnectionString(configuration["NotificationHubConnection"],
            "kinaunanotifications");

        public async Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "")
        {

            JObject payload = new(
                new JProperty("data", new JObject(new JProperty("title", title), new JProperty("message", message))),
                new JProperty("notData", timeLineItem.TimeLineId));

            List<UserAccess> userList = await dataService.GetProgenyUserAccessList(timeLineItem.ProgenyId);
            foreach (UserAccess userAcces in userList)
            {
                if (userAcces.AccessLevel > timeLineItem.AccessLevel) continue;

                UserInfo userInfo = await dataService.GetUserInfoByEmail(userAcces.UserId);
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
                await dataService.AddMobileNotification(notification);

                string userTag = "userEmail:" + userAcces.UserId.ToUpper();
                await Hub.SendFcmNativeNotificationAsync(payload.ToString(Newtonsoft.Json.Formatting.None), userTag);
            }
        }
    }
}
