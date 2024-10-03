using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public interface ICalendarRemindersService
    {
        Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder);
        Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder);
        Task<List<CalendarReminder>> GetAllCalendarReminders();
        Task<CalendarReminder> GetCalendarReminder(int id);
        Task<List<CalendarReminder>> GetCalendarRemindersForUser(string userId);
        Task<List<CalendarReminder>> GetExpiredCalendarReminders();
        Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder);
    }
}