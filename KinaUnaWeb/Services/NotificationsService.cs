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
                        WebNotification webNotification = new()
                        {
                            To = uaUserInfo.UserId,
                            From = currentUser.FullName(),
                            Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context,
                            DateTime = DateTime.UtcNow,
                            Icon = currentUser.ProfilePicture,
                            Title = "A new contact was added for " + progeny.NickName,
                            Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + progeny.Id,
                            Type = "Notification"
                        };

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
                        WebNotification notification = new()
                        {
                            To = uaUserInfo.UserId,
                            From = currentUser.FullName(),
                            Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context,
                            DateTime = DateTime.UtcNow,
                            Icon = currentUser.ProfilePicture,
                            Title = "A new friend was added for " + progeny.NickName,
                            Link = "/Friends?childId=" + progeny.Id,
                            Type = "Notification"
                        };

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
                        WebNotification webNotification = new()
                        {
                            To = uaUserInfo.UserId,
                            From = currentUser.FullName(),
                            Message = "Name: " + locationItem.Name + "\r\nDate: " + dateString,
                            DateTime = DateTime.UtcNow,
                            Icon = currentUser.ProfilePicture,
                            Title = "A new location was added for " + progeny.NickName,
                            Link = "/Locations?childId=" + progeny.Id,
                            Type = "Notification"
                        };

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
                        WebNotification notification = new()
                        {
                            To = uaUserInfo.UserId,
                            From = currentUser.FullName(),
                            Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight,
                            DateTime = DateTime.UtcNow,
                            Icon = currentUser.ProfilePicture,
                            Title = "A new measurement was added for " + progeny.NickName,
                            Link = "/Measurements?childId=" + progeny.Id,
                            Type = "Notification"
                        };

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + progeny.Id);
                    }
                }
            }
        }

        public async Task SendNoteNotification(Note noteItem, UserInfo currentUser)
        {
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(noteItem.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(noteItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= noteItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new()
                        {
                            To = uaUserInfo.UserId,
                            From = currentUser.FullName(),
                            Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category,
                            DateTime = DateTime.UtcNow,
                            Icon = currentUser.ProfilePicture,
                            Title = "A new note was added for " + progeny.NickName,
                            Link = "/Notes?childId=" + progeny.Id,
                            Type = "Notification"
                        };

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + progeny.Id);
                    }
                }
            }
        }
    }
}
