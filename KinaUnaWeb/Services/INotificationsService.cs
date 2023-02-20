using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services;

public interface INotificationsService
{
    Task SendCalendarNotification(CalendarItem eventItem, UserInfo currentUser);
    Task SendContactNotification(Contact contactItem, UserInfo currentUser);
    Task SendFriendNotification(Friend friendItem, UserInfo currentUser);
    Task SendLocationNotification(Location locationItem, UserInfo currentUser);
    Task SendMeasurementNotification(Measurement measurementItem, UserInfo currentUser);
}