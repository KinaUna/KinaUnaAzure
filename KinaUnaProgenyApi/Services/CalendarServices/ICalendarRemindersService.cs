using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public interface ICalendarRemindersService
    {
        Task<CustomResult<CalendarReminder>> AddCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo);
        Task<CustomResult<CalendarReminder>> DeleteCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo);
        Task<List<CalendarReminder>> GetAllCalendarReminders();
        Task<CustomResult<CalendarReminder>> GetCalendarReminder(int id, UserInfo userInfo);
        Task<CustomResult<List<CalendarReminder>>> GetCalendarRemindersForUser(CalendarRemindersForUserRequest request, UserInfo userInfo);
        Task<List<CalendarReminder>> GetExpiredCalendarReminders();
        Task<CustomResult<CalendarReminder>> UpdateCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo);
        Task SendCalendarReminder(int id);
    }
}