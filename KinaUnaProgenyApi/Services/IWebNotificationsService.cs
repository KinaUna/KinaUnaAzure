using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

public interface IWebNotificationsService
{
    Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser, string title);
    Task SendContactNotification(Contact contactItem, UserInfo currentUser, string title);
    Task SendFriendNotification(Friend friendItem, UserInfo currentUser, string title);
    Task SendLocationNotification(Location locationItem, UserInfo currentUser, string title);
    Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser, string title);
    Task SendNoteNotification(Note noteItem, UserInfo currentUser, string title);
    Task SendPictureNotification(Picture pictureItem, UserInfo currentUser, string title);
    Task SendVideoNotification(Video videoItem, UserInfo currentUser, string title);
    Task SendSkillNotification(Skill skillItem, UserInfo currentUser, string title);
    Task SendCommentNotification(Comment commentItem, UserInfo currentUser, string title, string message);
    Task SendSleepNotification(Sleep sleepItem, UserInfo currentUser, string title);
    Task SendVaccinationNotification(Vaccination vaccinationItem, UserInfo currentUser, string title);
    Task SendVocabularyNotification(VocabularyItem vocabularyItem, UserInfo userInfo, string title);
    Task SendUserAccessNotification(UserAccess userAccessItem, UserInfo userInfo, string title);
}