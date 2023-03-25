using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public class WebNotificationsService : IWebNotificationsService
    {
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IDataService _dataService;
        private readonly IUserAccessService _userAccessService;
        private readonly IUserInfoService _userInfoService;
        public WebNotificationsService(IPushMessageSender pushMessageSender, IDataService dataService, IUserAccessService userAccessService, IUserInfoService userInfoService)
        {
            _pushMessageSender = pushMessageSender;
            _dataService = dataService;
            _userAccessService = userAccessService;
            _userInfoService = userInfoService;
        }

        public async Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(eventItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= eventItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
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
                            webNotification.Title = title;
                            webNotification.Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + eventItem.ProgenyId;
                            webNotification.Type = "Notification";

                            webNotification = await _dataService.AddWebNotification(webNotification);

                            await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                                webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacalendar" + eventItem.ProgenyId);
                        }
                    }
                }
            }
        }

        public async Task SendContactNotification(Contact contactItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(contactItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= contactItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification webNotification = new WebNotification();
                        webNotification.To = uaUserInfo.UserId;
                        webNotification.From = currentUser.FullName();
                        webNotification.Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context;
                        webNotification.DateTime = DateTime.UtcNow;
                        webNotification.Icon = currentUser.ProfilePicture;
                        webNotification.Title = title;
                        webNotification.Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + contactItem.ProgenyId;
                        webNotification.Type = "Notification";

                        webNotification = await _dataService.AddWebNotification(webNotification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                            webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacontact" + contactItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendFriendNotification(Friend friendItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(friendItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= friendItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Friends?childId=" + friendItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + friendItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendLocationNotification(Location locationItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await _userAccessService.GetProgenyUserAccessList(locationItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= locationItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
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
                        webNotification.Title = title;
                        webNotification.Link = "/Locations?childId=" + locationItem.ProgenyId;
                        webNotification.Type = "Notification";

                        webNotification = await _dataService.AddWebNotification(webNotification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                            webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunalocation" + locationItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await _userAccessService.GetProgenyUserAccessList(measurementItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= measurementItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Measurements?childId=" + measurementItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + measurementItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendNoteNotification(Note noteItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await _userAccessService.GetProgenyUserAccessList(noteItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= noteItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Notes?childId=" + noteItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + noteItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendPictureNotification(Picture pictureItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(pictureItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= pictureItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        string picTimeString;
                        if (pictureItem.PictureTime.HasValue)
                        {
                            DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(pictureItem.PictureTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            picTimeString = "Photo taken: " + picTime.ToString("dd-MMM-yyyy HH:mm");
                        }
                        else
                        {
                            picTimeString = "Photo taken: Unknown";
                        }
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = picTimeString + "\r\n";
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Pictures/Picture/" + pictureItem.PictureId + "?childId=" + pictureItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaphoto" + pictureItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendVideoNotification(Video videoItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(videoItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= videoItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        string picTimeString;
                        if (videoItem.VideoTime.HasValue)
                        {
                            DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(videoItem.VideoTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            picTimeString = "Video recorded: " + picTime.ToString("dd-MMM-yyyy HH:mm");
                        }
                        else
                        {
                            picTimeString = "Video recorded: Unknown";
                        }
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = picTimeString + "\r\n";
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Videos/Video/" + videoItem.VideoId + "?childId=" + videoItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavideo" + videoItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendSkillNotification(Skill skillItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(skillItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= skillItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        if (!skillItem.SkillFirstObservation.HasValue)
                        {
                            skillItem.SkillFirstObservation = DateTime.UtcNow;
                        }

                        string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy");

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Skills?childId=" + skillItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + skillItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendCommentNotification(Comment commentItem, UserInfo currentUser, string title, string message)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(commentItem.Progeny.Id);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= commentItem.Progeny.Id)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification webNotification = new WebNotification();
                        webNotification.To = uaUserInfo.UserId;
                        webNotification.From = currentUser.FullName();
                        webNotification.Message = message;
                        webNotification.DateTime = DateTime.UtcNow;
                        webNotification.Icon = currentUser.ProfilePicture;
                        webNotification.Title = title;
                        string tagString = string.Empty;
                        if (commentItem.ItemType == (int)KinaUnaTypes.TimeLineType.Photo)
                        {
                            webNotification.Link = "/Pictures/Picture/" + commentItem.ItemId + "?childId=" + commentItem.Progeny.Id;
                            tagString = "kinaunaphoto";
                        }

                        if (commentItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                        {
                            webNotification.Link = "/Videos/Video/" + commentItem.ItemId + "?childId=" + commentItem.Progeny.Id;
                            tagString = "kinaunavideo";
                        }

                        webNotification.Type = "Notification";

                        webNotification = await _dataService.AddWebNotification(webNotification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                            webNotification.Message, Constants.WebAppUrl + webNotification.Link, tagString + commentItem.Progeny.Id);
                    }
                }
            }
        }

        public async Task SendSleepNotification(Sleep sleepItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(sleepItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= sleepItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        DateTime sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        DateTime sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " + sleepEnd.ToString("dd-MMM-yyyy HH:mm");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Sleep?childId=" + sleepItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                            Constants.WebAppUrl + notification.Link, "kinaunasleep" + sleepItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendVaccinationNotification(Vaccination vaccinationItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await _userAccessService.GetProgenyUserAccessList(vaccinationItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel <= vaccinationItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = currentUser.FullName();
                        notification.Message = "Name: " + vaccinationItem.VaccinationName + "\r\nContext: " + vaccinationItem.VaccinationDate.ToString("dd-MMM-yyyy");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = currentUser.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Vaccinations?childId=" + vaccinationItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + vaccinationItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo userInfo, string title)
        {
            List<UserAccess> usersToNotif = await _userAccessService.GetProgenyUserAccessList(vocabularyItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= vocabularyItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        string vocabTimeString = String.Empty;
                        if (vocabularyItem.Date.HasValue)
                        {
                            DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                            vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy");
                        }

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = userInfo.FullName();
                        notification.Message = "Word: " + vocabularyItem.Word + "\r\nLanguage: " + vocabularyItem.Language + vocabTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userInfo.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Vocabulary?childId=" + vocabularyItem.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavocabulary" + vocabularyItem.ProgenyId);
                    }
                }
            }
        }

        public async Task SendUserAccessNotification(UserAccess userAccessItem, UserInfo userInfo, string title)
        {
            List<UserAccess> usersToNotif = await _userAccessService.GetProgenyUserAccessList(userAccessItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel == 0)
                {
                    UserInfo uaUserInfo = await _userInfoService.GetUserInfoByEmail(userAccess.UserId);
                    if (uaUserInfo != null && uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = userInfo.FullName();
                        notification.Message = "User email: " + userAccessItem.UserId;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userInfo.ProfilePicture;
                        notification.Title = title;
                        notification.Link = "/Family";
                        notification.Type = "Notification";

                        notification = await _dataService.AddWebNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunauseraccess" + userAccessItem.ProgenyId);
                    }
                }
            }
        }
    }
}
