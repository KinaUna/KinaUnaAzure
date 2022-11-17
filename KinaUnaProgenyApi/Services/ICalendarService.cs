using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ICalendarService
    {
        Task<CalendarItem> GetCalendarItem(int id);
        Task<CalendarItem> AddCalendarItem(CalendarItem item);
        Task<CalendarItem> UpdateCalendarItem(CalendarItem item);
        Task<CalendarItem> DeleteCalendarItem(CalendarItem item);
        Task<List<CalendarItem>> GetCalendarList(int progenyId);
    }
}
