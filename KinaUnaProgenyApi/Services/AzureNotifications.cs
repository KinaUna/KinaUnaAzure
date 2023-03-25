using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace KinaUnaProgenyApi.Services
{
    public class AzureNotifications : IAzureNotifications
    {
        private readonly IDataService _dataService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        public NotificationHubClient Hub { get; set; }

        public AzureNotifications(IConfiguration configuration, IDataService dataService, IUserInfoService userInfoService, IUserAccessService userAccessService)
        {
            Hub = NotificationHubClient.CreateClientFromConnectionString(configuration["NotificationHubConnection"],
                "kinaunanotifications");
            _dataService = dataService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
        }

        public async Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "")
        {

            JObject payload = new JObject(
                new JProperty("data", new JObject(new JProperty("title", title), new JProperty("message", message))),
                new JProperty("notData", timeLineItem.TimeLineId));

            string alert = "{\"aps\":{\"alert\":\"" + message + "\"},\"message\":\"" + message + "\",\"notData\":\"" + timeLineItem.TimeLineId + "\", \"content-available\":1}";

            List<UserAccess> userList = await _userAccessService.GetProgenyUserAccessList(timeLineItem.ProgenyId);
            foreach (UserAccess userAcces in userList)
            {
                if (userAcces.AccessLevel <= timeLineItem.AccessLevel)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userAcces.UserId);
                    if (userInfo != null)
                    {
                        MobileNotification notification = new MobileNotification();

                        notification.UserId = userInfo.UserId;
                        notification.IconLink = iconLink;
                        notification.ItemId = timeLineItem.ItemId;
                        notification.ItemType = timeLineItem.ItemType;
                        notification.Language = "EN";
                        notification.Message = message;
                        notification.Title = title;
                        notification.Time = DateTime.UtcNow;
                        notification.Read = false;
                        _ = await _dataService.AddMobileNotification(notification);


                        string userTag = "userEmail:" + userAcces.UserId.ToUpper();
                        _ = await Hub.SendFcmNativeNotificationAsync(payload.ToString(Newtonsoft.Json.Formatting.None), userTag);
                        _ = await Hub.SendAppleNativeNotificationAsync(alert, userTag);
                    }
                }
            }

        }
    }
}
