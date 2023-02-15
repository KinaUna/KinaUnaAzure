using KinaUna.Data.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;

namespace KinaUnaWeb.Services
{
    public class NotificationsService : INotificationsService
    {
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;

        public NotificationsService(IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService, IUserAccessHttpClient userAccessHttpClient,
            IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient)
        {
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
            _userAccessHttpClient = userAccessHttpClient;
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
        }

        public async Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(eventItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= eventItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        if (eventItem.StartTime != null)
                        {
                            DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");

                            if (eventItem.EndTime != null)
                            {
                                DateTime endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                                    TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                                eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                            }

                            WebNotification webNotification = new WebNotification();
                            webNotification.To = uaUserInfo.UserId;
                            webNotification.From = currentUser.FullName();
                            webNotification.Message = eventItem.Title + eventTimeString;
                            webNotification.DateTime = DateTime.UtcNow;
                            webNotification.Icon = currentUser.ProfilePicture;
                            webNotification.Title = "A new calendar event was added for " + progeny.NickName;
                            webNotification.Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + progeny.Id;
                            webNotification.Type = "Notification";

                            webNotification = await _webNotificationsService.SaveNotification(webNotification);

                            await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                                webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacalendar" + progeny.Id);
                        }
                    }
                }
            }
        }
    }
}
