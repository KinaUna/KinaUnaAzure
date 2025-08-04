using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.UserAccessService;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// This service is used to send notifications and add the notification to the database, for each relevant user, when items are changed or added.
    /// </summary>
    /// <param name="pushMessageSender"></param>
    /// <param name="notificationsService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="userInfoService"></param>
    public class WebNotificationsService(IPushMessageSender pushMessageSender, INotificationsService notificationsService, IUserAccessService userAccessService, IUserInfoService userInfoService)
        : IWebNotificationsService
    {
        /// <summary>
        /// Adds a notification for a CalendarItem to the database, and sends a push notification (if they registered for it), for all users with access to the CalendarItem.
        /// Adds the start and end time in the recipient's timezone to the message body.
        /// </summary>
        /// <param name="eventItem">The CalendarItem that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        /// <returns></returns>
        public async Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(eventItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult.IsFailure) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
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
                    Link = "/Calendar?eventId=" + eventItem.EventId + "&childId=" + eventItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacalendar" + eventItem.EventId);
            }
        }

        /// <summary>
        /// Adds a notification for a Contact to the database, and sends a push notification (if they registered for it), for all users with access to the Contact.
        /// </summary>
        /// <param name="contactItem">The Contact that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        /// <returns></returns>
        public async Task SendContactNotification(Contact contactItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(contactItem.ProgenyId, Constants.SystemAccountEmail);

            if(usersToNotifyResult.IsFailure) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > contactItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification webNotification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context, // Todo: Translation of Name and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Contacts?contactId=" + contactItem.ContactId + "&childId=" + contactItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunacontact" + contactItem.ContactId);
            }
        }

        /// <summary>
        /// Adds a notification for a Friend to the database, and sends a push notification (if they registered for it), for all users with access to the Friend.
        /// </summary>
        /// <param name="friendItem">The Friend that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        /// <returns></returns>
        public async Task SendFriendNotification(Friend friendItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(friendItem.ProgenyId, Constants.SystemAccountEmail);

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > friendItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context, // Todo: Translation of Friend and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Friends?friendId=" + friendItem.FriendId + "&childId=" + friendItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + friendItem.FriendId);
            }
        }

        /// <summary>
        /// Adds a notification for a Location to the database, and sends a push notification (if they registered for it), for all users with access to the Location.
        /// Adds the date in the recipient's timezone to the message body.
        /// </summary>
        /// <param name="locationItem">The Location that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        /// <returns></returns>
        public async Task SendLocationNotification(Location locationItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifResult = await userAccessService.GetProgenyUserAccessList(locationItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifResult.IsFailure) return;

            foreach (UserAccess userAccess in usersToNotifResult.Value)
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
                    Message = "Name: " + locationItem.Name + "\r\nDate: " + dateString, // Todo: Translation of Name and Date
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Locations?locationId=" + locationItem.LocationId + "&childId=" + locationItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunalocation" + locationItem.LocationId);
            }
        }

        /// <summary>
        /// Adds a notification for a Measurement to the database, and sends a push notification (if they registered for it), for all users with access to the Measurement.
        /// </summary>
        /// <param name="measurementItem">The Measurement that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifResult = await userAccessService.GetProgenyUserAccessList(measurementItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifResult == null) return;

            foreach (UserAccess userAccess in usersToNotifResult.Value)
            {
                if (userAccess.AccessLevel > measurementItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight, // Todo: Translation of Height and Weight
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Measurements?measurementId=" + measurementItem.MeasurementId + "&childId=" + measurementItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + measurementItem.MeasurementId);
            }
        }

        /// <summary>
        /// Adds a notification for a Note to the database, and sends a push notification (if they registered for it), for all users with access to the Note.
        /// </summary>
        /// <param name="noteItem">The Note that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendNoteNotification(Note noteItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifResult = await userAccessService.GetProgenyUserAccessList(noteItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifResult == null) return;

            foreach (UserAccess userAccess in usersToNotifResult.Value)
            {
                if (userAccess.AccessLevel > noteItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category, // Todo: Translation of Title and Category
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Notes?noteId=" + noteItem.NoteId + "&childId=" + noteItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + noteItem.NoteId);
            }
        }

        /// <summary>
        /// Adds a notification for a Picture to the database, and sends a push notification (if they registered for it), for all users with access to the Picture.
        /// </summary>
        /// <param name="pictureItem">The Picture that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendPictureNotification(Picture pictureItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(pictureItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > pictureItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                string picTimeString;
                if (pictureItem.PictureTime.HasValue)
                {
                    DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(pictureItem.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    picTimeString = "Photo taken: " + picTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Translation of Photo taken
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
                    Link = "/Pictures?childId=" + pictureItem.ProgenyId + "&pictureId=" + pictureItem.PictureId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaphoto" + pictureItem.PictureId);
            }
        }

        /// <summary>
        /// Adds a notification for a Video to the database, and sends a push notification (if they registered for it), for all users with access to the Video.
        /// </summary>
        /// <param name="videoItem">The Video that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendVideoNotification(Video videoItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(videoItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > videoItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                string picTimeString;
                if (videoItem.VideoTime.HasValue)
                {
                    DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(videoItem.VideoTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    picTimeString = "Video recorded: " + picTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Translation of Video recorded
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
                    Link = "/Videos?videoId=" + videoItem.VideoId + "&childId=" + videoItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavideo" + videoItem.VideoId);
            }
        }

        /// <summary>
        /// Adds a notification for a Skill to the database, and sends a push notification (if they registered for it), for all users with access to the Skill.
        /// </summary>
        /// <param name="skillItem">The Skill that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendSkillNotification(Skill skillItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(skillItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > skillItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                skillItem.SkillFirstObservation ??= DateTime.UtcNow;

                string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy"); // Todo: Translation of Date

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Skills?skillId=" + skillItem.SkillId + "&childId=" + skillItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + skillItem.SkillId);
            }
        }

        /// <summary>
        /// Adds a notification for a Comment to the database, and sends a push notification (if they registered for it), for all users with access to the Comment.
        /// </summary>
        /// <param name="commentItem">The Comment that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="message">The message body of the notification.</param>
        public async Task SendCommentNotification(Comment commentItem, UserInfo currentUser, string title, string message)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(commentItem.Progeny.Id, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
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
                    webNotification.Link = "/Pictures?pictureId=" + commentItem.ItemId + "&childId=" + commentItem.Progeny.Id;
                    tagString = "kinaunaphoto";
                }

                if (commentItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                {
                    webNotification.Link = "/Videos?videoId=" + commentItem.ItemId + "&childId=" + commentItem.Progeny.Id;
                    tagString = "kinaunavideo";
                }

                webNotification.Type = "Notification";

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, tagString + commentItem.CommentId);
            }
        }

        /// <summary>
        /// Adds a notification for a Sleep item to the database, and sends a push notification (if they registered for it), for all users with access to the Sleep item.
        /// </summary>
        /// <param name="sleepItem">The Sleep item that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendSleepNotification(Sleep sleepItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(sleepItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
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
                    Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " + sleepEnd.ToString("dd-MMM-yyyy HH:mm"), // Todo: Translation of Start and End
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Sleep?sleepId=" + sleepItem.SleepId + "&childId=" + sleepItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                    Constants.WebAppUrl + notification.Link, "kinaunasleep" + sleepItem.SleepId);
            }
        }

        /// <summary>
        /// Adds a notification for a Vaccination item to the database, and sends a push notification (if they registered for it), for all users with access to the Vaccination item.
        /// </summary>
        /// <param name="vaccinationItem">The Vaccination item that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendVaccinationNotification(Vaccination vaccinationItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(vaccinationItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult == null) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > vaccinationItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + vaccinationItem.VaccinationName + "\r\nDate: " + vaccinationItem.VaccinationDate.ToString("dd-MMM-yyyy"), // Todo: Translation of Name and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Vaccinations?vaccinationId=" + vaccinationItem.VaccinationId + "&childId=" + vaccinationItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + vaccinationItem.VaccinationId);
            }
        }

        /// <summary>
        /// Adds a notification for a VocabularyItem to the database, and sends a push notification (if they registered for it), for all users with access to the VocabularyItem.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem that was added, updated, or deleted.</param>
        /// <param name="userInfo">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo userInfo, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifResult = await userAccessService.GetProgenyUserAccessList(vocabularyItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifResult == null) return;

            foreach (UserAccess userAccess in usersToNotifResult.Value)
            {
                if (userAccess.AccessLevel > vocabularyItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                string vocabTimeString = string.Empty;
                if (vocabularyItem.Date.HasValue)
                {
                    DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                    vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy"); // Todo: Translation of Date
                }

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = userInfo.FullName(),
                    Message = "Word: " + vocabularyItem.Word + "\r\nLanguage: " + vocabularyItem.Language + vocabTimeString, // Todo: Translation of Word and Language
                    DateTime = DateTime.UtcNow,
                    Icon = userInfo.ProfilePicture,
                    Title = title,
                    Link = "/Vocabulary?vocabularyId=" + vocabularyItem.WordId + "&childId=" + vocabularyItem.ProgenyId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavocabulary" + vocabularyItem.WordId);
            }
        }

        /// <summary>
        /// Adds a notification for a UserAccess item to the database, and sends a push notification (if they registered for it), for all users with admin access to the Progeny that the item belongs to.
        /// </summary>
        /// <param name="userAccessItem">The UserAccess item that was added, updated, or deleted.</param>
        /// <param name="userInfo">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendUserAccessNotification(UserAccess userAccessItem, UserInfo userInfo, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifResult = await userAccessService.GetProgenyUserAccessList(userAccessItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifResult == null) return;

            foreach (UserAccess userAccess in usersToNotifResult.Value)
            {
                if (userAccess.AccessLevel != 0) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                WebNotification notification = new()
                {
                    To = uaUserInfo.UserId,
                    From = userInfo.FullName(),
                    Message = "User email: " + userAccessItem.UserId, // Todo: Translation of User email
                    DateTime = DateTime.UtcNow,
                    Icon = userInfo.ProfilePicture,
                    Title = title,
                    Link = "/Family",
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunauseraccess" + userAccessItem.ProgenyId);
            }
        }

        public async Task SendTodoItemNotification(TodoItem todoItem, UserInfo currentUser, string title)
        {
            CustomResult<List<UserAccess>> usersToNotifyResult = await userAccessService.GetProgenyUserAccessList(todoItem.ProgenyId, Constants.SystemAccountEmail);

            if (usersToNotifyResult.IsFailure) return;

            foreach (UserAccess userAccess in usersToNotifyResult.Value)
            {
                if (userAccess.AccessLevel > todoItem.AccessLevel) continue;

                UserInfo uaUserInfo = await userInfoService.GetUserInfoByEmail(userAccess.UserId);
                if (uaUserInfo == null || uaUserInfo.UserId == "Unknown") continue;

                string eventTimeString;
                if (todoItem.StartDate != null)
                {
                    DateTime startDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.StartDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    eventTimeString = "\r\nStart: " + startDate.ToString("dd-MMM-yyyy");
                }
                else
                {
                    DateTime startDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    eventTimeString = "\r\nStart: " + startDate.ToString("dd-MMM-yyyy");
                }
                

                if (todoItem.DueDate != null)
                {
                    DateTime dueDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                    eventTimeString = eventTimeString + "\r\nDue: " + dueDate.ToString("dd-MMM-yyyy");
                }

                WebNotification webNotification = new()
                {
                    To = uaUserInfo.UserId,
                    From = currentUser.FullName(),
                    Message = todoItem.Title + eventTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Todo?todoItemId=" + todoItem.TodoItemId + "&childId=" + todoItem.ProgenyId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(uaUserInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunatodolist" + todoItem.TodoItemId);
            }
        }
    }
}