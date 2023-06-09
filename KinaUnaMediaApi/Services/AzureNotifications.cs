﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace KinaUnaMediaApi.Services
{
    public class AzureNotifications
    {
        private readonly IDataService _dataService;
        
        public NotificationHubClient Hub { get; set; }

        public AzureNotifications(IConfiguration configuration, IDataService dataService)
        {
            Hub = NotificationHubClient.CreateClientFromConnectionString(configuration["NotificationHubConnection"],
                "kinaunanotifications");
            _dataService = dataService;
            
        }

        public async Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "")
        {

            JObject payload = new JObject(
                new JProperty("data", new JObject(new JProperty("title", title), new JProperty("message", message))),
                new JProperty("notData", timeLineItem.TimeLineId));

            List<UserAccess> userList = await _dataService.GetProgenyUserAccessList(timeLineItem.ProgenyId);
            foreach (UserAccess userAcces in userList)
            {
                if (userAcces.AccessLevel <= timeLineItem.AccessLevel)
                {
                    UserInfo userInfo = await _dataService.GetUserInfoByEmail(userAcces.UserId);
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
                        await _dataService.AddMobileNotification(notification);

                        string userTag = "userEmail:" + userAcces.UserId.ToUpper();
                        await Hub.SendFcmNativeNotificationAsync(payload.ToString(Newtonsoft.Json.Formatting.None), userTag);
                    }
                }
            }
            // Android
            
        }
    }
}
