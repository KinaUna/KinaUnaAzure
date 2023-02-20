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
            List<UserAccess> usersToNotify = await _userAccessHttpClient.GetProgenyAccessList(eventItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            foreach (UserAccess ua in usersToNotify)
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

        public async Task SendContactNotification(Contact contactItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotify = await _userAccessHttpClient.GetProgenyAccessList(contactItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(contactItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= contactItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification webNotification = new WebNotification();
                        webNotification.To = uaUserInfo.UserId;
                        webNotification.From = currentUser.FullName();
                        webNotification.Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context;
                        webNotification.DateTime = DateTime.UtcNow;
                        webNotification.Icon = currentUser.ProfilePicture;
                        webNotification.Title = "A new contact was added for " + progeny.NickName;
                        webNotification.Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + progeny.Id;
                        webNotification.Type = "Notification";

                        webNotification = await _webNotificationsService.SaveNotification(webNotification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                            webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacontact" + progeny.Id);
                    }
                }
            }
        }

        public async Task SendFriendNotification(Friend friendItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotify = await _userAccessHttpClient.GetProgenyAccessList(friendItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(friendItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= friendItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = "A new friend was added for " + progeny.NickName;
                        notification.Link = "/Friends?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + progeny.Id);
                    }
                }
            }
        }

        public async Task SendLocationNotification(Location locationItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(locationItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(locationItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= locationItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime tempDate = DateTime.UtcNow;
                        if (locationItem.Date.HasValue)
                        {
                            tempDate = TimeZoneInfo.ConvertTimeFromUtc(locationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        }

                        string dateString = tempDate.ToString("dd-MMM-yyyy");
                        WebNotification webNotification = new WebNotification();
                        webNotification.To = uaUserInfo.UserId;
                        webNotification.From = currentUser.FullName();
                        webNotification.Message = "Name: " + locationItem.Name + "\r\nDate: " + dateString;
                        webNotification.DateTime = DateTime.UtcNow;
                        webNotification.Icon = currentUser.ProfilePicture;
                        webNotification.Title = "A new location was added for " + progeny.NickName;
                        webNotification.Link = "/Locations?childId=" + progeny.Id;
                        webNotification.Type = "Notification";

                        webNotification = await _webNotificationsService.SaveNotification(webNotification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                            webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunalocation" + progeny.Id);
                    }
                }
            }
        }

        public async Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(measurementItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(measurementItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= measurementItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = "A new measurement was added for " + progeny.NickName;
                        notification.Link = "/Measurements?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + progeny.Id);
                    }
                }
            }
        }
    }
}
