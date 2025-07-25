using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ICalendarRemindersHttpClient
    {
        Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder);
        Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder);
        Task<CalendarReminder> GetCalendarReminder(int reminderId);
        Task<List<CalendarReminder>> GetUsersCalendarRemindersForEvent(int eventId, string userId);
        Task<List<CalendarReminder>> GetCalendarRemindersForUser(string userId, bool filterNotified);
        Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder);
    }
}