using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ICalendarRemindersHttpClient
    {
        Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder);
        Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder);
        Task<CalendarReminder> GetCalendarReminder(int reminderId);
        Task<List<CalendarReminder>> GetCalendarRemindersForEvent(int eventId);
        Task<List<CalendarReminder>> GetCalendarRemindersForUser(string userId, bool filterNotified);
        Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder);
    }
}