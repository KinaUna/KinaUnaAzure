using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public class WebNotificationsService(IPushMessageSender pushMessageSender, IDataService dataService, IUserAccessService userAccessService, IUserInfoService userInfoService)
        : IWebNotificationsService
    {
        public async Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(eventItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > eventItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                if (eventItem.StartTime == null) continue;

                DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");

                if (eventItem.EndTime != null)
                {
                    DateTime endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                }

                WebNotification webNotification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = eventItem.Title + eventTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + eventItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await dataService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacalendar" + eventItem.ProgenyId);
            }
        }

        public async Task SendContactNotification(Contact contactItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(contactItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > contactItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification webNotification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + contactItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await dataService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacontact" + contactItem.ProgenyId);
            }
        }

        public async Task SendFriendNotification(Friend friendItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(friendItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > friendItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Friends/ViewFriend?friendId=" + friendItem.FriendId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + friendItem.ProgenyId);
            }
        }

        public async Task SendLocationNotification(Location locationItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await userAccessService.GetProgenyUserAccessList(locationItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel > locationItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

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
                    Title = title,
                    Link = "/Locations?childId=" + locationItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await dataService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunalocation" + locationItem.ProgenyId);
            }
        }

        public async Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await userAccessService.GetProgenyUserAccessList(measurementItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel > measurementItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Measurements?childId=" + measurementItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + measurementItem.ProgenyId);
            }
        }

        public async Task SendNoteNotification(Note noteItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotif = await userAccessService.GetProgenyUserAccessList(noteItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel > noteItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Notes/ViewNote?noteId=" + noteItem.NoteId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + noteItem.ProgenyId);
            }
        }

        public async Task SendPictureNotification(Picture pictureItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(pictureItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > pictureItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

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
                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = picTimeString + "\r\n",
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Pictures/Picture/" + pictureItem.PictureId + "?childId=" + pictureItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaphoto" + pictureItem.ProgenyId);
            }
        }

        public async Task SendVideoNotification(Video videoItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(videoItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > videoItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

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
                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = picTimeString + "\r\n",
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Videos/Video/" + videoItem.VideoId + "?childId=" + videoItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavideo" + videoItem.ProgenyId);
            }
        }

        public async Task SendSkillNotification(Skill skillItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(skillItem.ProgenyId);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > skillItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                skillItem.SkillFirstObservation ??= DateTime.UtcNow;

                string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy");

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Skills?childId=" + skillItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + skillItem.ProgenyId);
            }
        }

        public async Task SendCommentNotification(Comment commentItem, UserInfo currentUser, string title, string message)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(commentItem.Progeny.Id);

            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > commentItem.Progeny.Id) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification webNotification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = message,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title
                };
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

                webNotification = await dataService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, tagString + commentItem.Progeny.Id);
            }
        }

        public async Task SendSleepNotification(Sleep sleepItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(sleepItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > sleepItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                DateTime sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                DateTime sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " + sleepEnd.ToString("dd-MMM-yyyy HH:mm"),
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Sleep/ViewSleep?itemId=" + sleepItem.SleepId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                    Constants.WebAppUrl + notification.Link, "kinaunasleep" + sleepItem.ProgenyId);
            }
        }

        public async Task SendVaccinationNotification(Vaccination vaccinationItem, UserInfo currentUser, string title)
        {
            List<UserAccess> usersToNotify = await userAccessService.GetProgenyUserAccessList(vaccinationItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotify)
            {
                if (userAccess.AccessLevel > vaccinationItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + vaccinationItem.VaccinationName + "\r\nContext: " + vaccinationItem.VaccinationDate.ToString("dd-MMM-yyyy"),
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Vaccinations?childId=" + vaccinationItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + vaccinationItem.ProgenyId);
            }
        }

        public async Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo userInfo, string title)
        {
            List<UserAccess> usersToNotif = await userAccessService.GetProgenyUserAccessList(vocabularyItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel > vocabularyItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                string vocabTimeString = string.Empty;
                if (vocabularyItem.Date.HasValue)
                {
                    DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                    vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy");
                }

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = userInfo.FullName(),
                    Message = "Word: " + vocabularyItem.Word + "\r\nLanguage: " + vocabularyItem.Language + vocabTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = userInfo.ProfilePicture,
                    Title = title,
                    Link = "/Vocabulary?childId=" + vocabularyItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavocabulary" + vocabularyItem.ProgenyId);
            }
        }

        public async Task SendUserAccessNotification(UserAccess userAccessItem, UserInfo userInfo, string title)
        {
            List<UserAccess> usersToNotif = await userAccessService.GetProgenyUserAccessList(userAccessItem.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel != 0) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = userInfo.FullName(),
                    Message = "User email: " + userAccessItem.UserId,
                    DateTime = DateTime.UtcNow,
                    Icon = userInfo.ProfilePicture,
                    Title = title,
                    Link = "/Family",
                    Type = "Notification"
                };

                notification = await dataService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunauseraccess" + userAccessItem.ProgenyId);
            }
        }
    }
}