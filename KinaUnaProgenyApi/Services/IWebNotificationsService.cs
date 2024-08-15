using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

/// <summary>
/// This service is used to send notifications and add the notification to the database, for each relevant user, when items are changed or added.
/// </summary>
public interface IWebNotificationsService
{
    /// <summary>
    /// Adds a notification for a CalendarItem to the database, and sends a push notification (if they registered for it), for all users with access to the CalendarItem.
    /// </summary>
    /// <param name="eventItem">The CalendarItem that was added, updated, or deleted. </param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    /// <returns></returns>
    Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Contact to the database, and sends a push notification (if they registered for it), for all users with access to the Contact.
    /// </summary>
    /// <param name="contactItem">The Contact that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    /// <returns></returns>
    Task SendContactNotification(Contact contactItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Friend to the database, and sends a push notification (if they registered for it), for all users with access to the Friend.
    /// </summary>
    /// <param name="friendItem">The Friend that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    /// <returns></returns>
    Task SendFriendNotification(Friend friendItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Location to the database, and sends a push notification (if they registered for it), for all users with access to the Location.
    /// Adds the date in the recipient's timezone to the message body.
    /// </summary>
    /// <param name="locationItem">The Location that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    /// <returns></returns>
    Task SendLocationNotification(Location locationItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Measurement to the database, and sends a push notification (if they registered for it), for all users with access to the Measurement.
    /// </summary>
    /// <param name="measurementItem">The Measurement that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Note to the database, and sends a push notification (if they registered for it), for all users with access to the Note.
    /// </summary>
    /// <param name="noteItem">The Note that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendNoteNotification(Note noteItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Picture to the database, and sends a push notification (if they registered for it), for all users with access to the Picture.
    /// </summary>
    /// <param name="pictureItem">The Picture that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendPictureNotification(Picture pictureItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Video to the database, and sends a push notification (if they registered for it), for all users with access to the Video.
    /// </summary>
    /// <param name="videoItem">The Video that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendVideoNotification(Video videoItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Skill to the database, and sends a push notification (if they registered for it), for all users with access to the Skill.
    /// </summary>
    /// <param name="skillItem">The Skill that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendSkillNotification(Skill skillItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Comment to the database, and sends a push notification (if they registered for it), for all users with access to the Comment.
    /// </summary>
    /// <param name="commentItem">The Comment that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    Task SendCommentNotification(Comment commentItem, UserInfo currentUser, string title, string message);

    /// <summary>
    /// Adds a notification for a Sleep item to the database, and sends a push notification (if they registered for it), for all users with access to the Sleep item.
    /// </summary>
    /// <param name="sleepItem">The Sleep item that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendSleepNotification(Sleep sleepItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a Vaccination item to the database, and sends a push notification (if they registered for it), for all users with access to the Vaccination item.
    /// </summary>
    /// <param name="vaccinationItem">The Vaccination item that was added, updated, or deleted.</param>
    /// <param name="currentUser">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendVaccinationNotification(Vaccination vaccinationItem, UserInfo currentUser, string title);

    /// <summary>
    /// Adds a notification for a VocabularyItem to the database, and sends a push notification (if they registered for it), for all users with access to the VocabularyItem.
    /// </summary>
    /// <param name="vocabularyItem">The VocabularyItem that was added, updated, or deleted.</param>
    /// <param name="userInfo">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo userInfo, string title);

    /// <summary>
    /// Adds a notification for a UserAccess item to the database, and sends a push notification (if they registered for it), for all users with admin access to the Progeny that the item belongs to.
    /// </summary>
    /// <param name="userAccessItem">The UserAccess item that was added, updated, or deleted.</param>
    /// <param name="userInfo">The UserInfo for the user who made changes.</param>
    /// <param name="title">The title of the notification.</param>
    Task SendUserAccessNotification(UserAccess userAccessItem, UserInfo userInfo, string title);
}