using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// This service is used to send notifications and add the notification to the database, for each relevant user, when items are changed or added.
    /// </summary>
    /// <param name="pushMessageSender"></param>
    /// <param name="notificationsService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="userInfoService"></param>
    public class WebNotificationsService(
        IPushMessageSender pushMessageSender,
        INotificationsService notificationsService,
        IUserAccessService userAccessService,
        IUserInfoService userInfoService,
        IAccessManagementService accessManagementService,
        IUserGroupsService userGroupsService,
        IWebHostEnvironment webHostEnvironment)
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Calendar, eventItem.EventId, currentUser);
            
            foreach (UserInfo userInfo in usersToNotify)
            {
                if (eventItem.StartTime == null) continue;

                DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");

                if (eventItem.EndTime != null)
                {
                    DateTime endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                }

                WebNotification webNotification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = eventItem.Title + eventTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Calendar?eventId=" + eventItem.EventId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(userInfo.UserId, webNotification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Contact, contactItem.ContactId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                WebNotification webNotification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context, // Todo: Translation of Name and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Contacts?contactId=" + contactItem.ContactId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(userInfo.UserId, webNotification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Friend, friendItem.FriendId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context, // Todo: Translation of Friend and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Friends?friendId=" + friendItem.FriendId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Location, locationItem.LocationId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                DateTime tempDate = DateTime.UtcNow;
                if (locationItem.Date.HasValue)
                {
                    tempDate = TimeZoneInfo.ConvertTimeFromUtc(locationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                }

                string dateString = tempDate.ToString("dd-MMM-yyyy");
                WebNotification webNotification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + locationItem.Name + "\r\nDate: " + dateString, // Todo: Translation of Name and Date
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Locations?locationId=" + locationItem.LocationId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(userInfo.UserId, webNotification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Measurement, measurementItem.MeasurementId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight, // Todo: Translation of Height and Weight
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Measurements?measurementId=" + measurementItem.MeasurementId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            //Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Note, noteItem.NoteId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category, // Todo: Translation of Title and Category
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Notes?noteId=" + noteItem.NoteId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Photo, pictureItem.PictureId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                string picTimeString;
                if (pictureItem.PictureTime.HasValue)
                {
                    DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(pictureItem.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    picTimeString = "Photo taken: " + picTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Translation of Photo taken
                }
                else
                {
                    picTimeString = "Photo taken: Unknown";
                }

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = picTimeString + "\r\n",
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Pictures?pictureId=" + pictureItem.PictureId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Video, videoItem.VideoId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                string picTimeString;
                if (videoItem.VideoTime.HasValue)
                {
                    DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(videoItem.VideoTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    picTimeString = "Video recorded: " + picTime.ToString("dd-MMM-yyyy HH:mm"); // Todo: Translation of Video recorded
                }
                else
                {
                    picTimeString = "Video recorded: Unknown";
                }

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = picTimeString + "\r\n",
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Videos?videoId=" + videoItem.VideoId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Skill, skillItem.SkillId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                skillItem.SkillFirstObservation ??= DateTime.UtcNow;

                string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy"); // Todo: Translation of Date

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Skills?skillId=" + skillItem.SkillId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }
            
            bool itemIdParsed = int.TryParse(commentItem.ItemId, out int itemId);
            if (!itemIdParsed) return;

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem((KinaUnaTypes.TimeLineType)commentItem.ItemType, itemId, currentUser);
            
            foreach (UserInfo userInfo in usersToNotify)
            {
                WebNotification webNotification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = message,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title
                };
                string tagString = string.Empty;
                if (commentItem.ItemType == (int)KinaUnaTypes.TimeLineType.Photo)
                {
                    webNotification.Link = "/Pictures?pictureId=" + commentItem.ItemId;
                    tagString = "kinaunaphoto";
                }

                if (commentItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                {
                    webNotification.Link = "/Videos?videoId=" + commentItem.ItemId;
                    tagString = "kinaunavideo";
                }

                webNotification.Type = "Notification";

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(userInfo.UserId, webNotification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Sleep, sleepItem.SleepId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                DateTime sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                DateTime sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " + sleepEnd.ToString("dd-MMM-yyyy HH:mm"), // Todo: Translation of Start and End
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Sleep?sleepId=" + sleepItem.SleepId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title, notification.Message,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Vaccination, vaccinationItem.VaccinationId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = "Name: " + vaccinationItem.VaccinationName + "\r\nDate: " + vaccinationItem.VaccinationDate.ToString("dd-MMM-yyyy"), // Todo: Translation of Name and Context
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Vaccinations?vaccinationId=" + vaccinationItem.VaccinationId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + vaccinationItem.VaccinationId);
            }
        }

        /// <summary>
        /// Adds a notification for a VocabularyItem to the database, and sends a push notification (if they registered for it), for all users with access to the VocabularyItem.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem that was added, updated, or deleted.</param>
        /// <param name="currentUser">The UserInfo for the user who made changes.</param>
        /// <param name="title">The title of the notification.</param>
        public async Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo currentUser, string title)
        {
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                string vocabTimeString = string.Empty;
                if (vocabularyItem.Date.HasValue)
                {
                    DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));

                    vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy"); // Todo: Translation of Date
                }

                WebNotification notification = new()
                {
                    To = userInfo.UserId,
                    From = userInfo.FullName(),
                    Message = "Word: " + vocabularyItem.Word + "\r\nLanguage: " + vocabularyItem.Language + vocabTimeString, // Todo: Translation of Word and Language
                    DateTime = DateTime.UtcNow,
                    Icon = userInfo.ProfilePicture,
                    Title = title,
                    Link = "/Vocabulary?vocabularyId=" + vocabularyItem.WordId,
                    Type = "Notification"
                };

                notification = await notificationsService.AddWebNotification(notification);

                await pushMessageSender.SendMessage(userInfo.UserId, notification.Title,
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
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

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

        /// <summary>
        /// Sends a notification to users with appropriate access to a specified to-do item.
        /// </summary>
        /// <remarks>This method retrieves the list of users with access to the progeny associated with
        /// the to-do item and sends a notification to each user whose access level permits viewing the item.
        /// Notifications include details such as the start and due dates of the to-do item, if available, and a link to
        /// the to-do item in the application.</remarks>
        /// <param name="todoItem">The to-do item for which notifications will be sent. Must not be null.</param>
        /// <param name="currentUser">The user initiating the notification. Must not be null.</param>
        /// <param name="title">The title of the notification. Must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation of sending notifications to users with access to the specified to-do item.</returns>
        public async Task SendTodoItemNotification(TodoItem todoItem, UserInfo currentUser, string title)
        {
            // Don't send for development environment, unless explicitly enabled.
            if (webHostEnvironment.IsDevelopment() && !Constants.SendNotificationsInDevelopment)
            {
                return;
            }

            List<UserInfo> usersToNotify = await GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, currentUser);

            foreach (UserInfo userInfo in usersToNotify)
            {
                string eventTimeString;
                if (todoItem.StartDate != null)
                {
                    DateTime startDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.StartDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    eventTimeString = "\r\nStart: " + startDate.ToString("dd-MMM-yyyy");
                }
                else
                {
                    DateTime startDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    eventTimeString = "\r\nStart: " + startDate.ToString("dd-MMM-yyyy");
                }


                if (todoItem.DueDate != null)
                {
                    DateTime dueDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                    eventTimeString = eventTimeString + "\r\nDue: " + dueDate.ToString("dd-MMM-yyyy");
                }

                WebNotification webNotification = new()
                {
                    To = userInfo.UserId,
                    From = currentUser.FullName(),
                    Message = todoItem.Title + eventTimeString,
                    DateTime = DateTime.UtcNow,
                    Icon = currentUser.ProfilePicture,
                    Title = title,
                    Link = "/Todos?todoItemId=" + todoItem.TodoItemId,
                    Type = "Notification"
                };

                webNotification = await notificationsService.AddWebNotification(webNotification);

                await pushMessageSender.SendMessage(userInfo.UserId, webNotification.Title,
                    webNotification.Message, Constants.WebAppUrl + webNotification.Link, "kinaunatodolist" + todoItem.TodoItemId);
            }
        }

        private async Task<List<UserInfo>> GetUsersToNotifyForItem(KinaUnaTypes.TimeLineType type, int itemId, UserInfo currentUser)
        {
            List<UserInfo> usersToNotify = [];
            List<TimelineItemPermission> permissions = await accessManagementService.GetTimelineItemPermissionsList(type, itemId, currentUser, true);
            foreach (TimelineItemPermission permission in permissions)
            {

                if (permission.InheritPermissions)
                {
                    // Get user groups and members
                    List<UserGroup> userGroups = [];
                    if (permission.ProgenyId > 0)
                    {
                        userGroups = await userGroupsService.GetUserGroupsForProgeny(permission.ProgenyId, currentUser, true);
                    }
                    else if (permission.FamilyId > 0)
                    {
                        userGroups = await userGroupsService.GetUserGroupsForFamily(permission.FamilyId, currentUser, true);
                    }

                    foreach (UserGroup userGroup in userGroups)
                    {
                        if (userGroup.PermissionLevel > PermissionLevel.None)
                        {
                            foreach (UserGroupMember member in userGroup.Members)
                            {
                                if (!string.IsNullOrEmpty(member.UserId) && !usersToNotify.Exists(u => u.UserId == member.UserId))
                                {
                                    UserInfo memberUserInfo = await userInfoService.GetUserInfoByUserId(member.UserId);
                                    usersToNotify.Add(memberUserInfo);
                                }
                            }
                        }
                    }
                }

                if (permission.PermissionLevel > PermissionLevel.None && permission.GroupId > 0)
                {
                    UserGroup userGroup = await userGroupsService.GetUserGroup(permission.GroupId, currentUser, true);
                    if (userGroup != null)
                    {
                        foreach (UserGroupMember member in userGroup.Members)
                        {
                            if (!string.IsNullOrEmpty(member.UserId) && !usersToNotify.Exists(u => u.UserId == member.UserId))
                            {
                                UserInfo memberUserInfo = await userInfoService.GetUserInfoByUserId(member.UserId);
                                usersToNotify.Add(memberUserInfo);
                            }
                        }
                    }
                }
            }

            return usersToNotify;
        }
    }
}