using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public class CalendarRemindersService(ProgenyDbContext context) : ICalendarRemindersService
    {
        public async Task<List<CalendarReminder>> GetAllCalendarReminders()
        {
            List<CalendarReminder> allCalendarReminders = await context.CalendarRemindersDb.AsNoTracking().ToListAsync();

            return allCalendarReminders;
        }

        public async Task<CalendarReminder> GetCalendarReminder(int id)
        {
            CalendarReminder calendarReminder = await context.CalendarRemindersDb.AsNoTracking().FirstOrDefaultAsync(c => c.CalendarReminderId == id);

            return calendarReminder;
        }

        public async Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder)
        {
            context.CalendarRemindersDb.Add(calendarReminder);
            await context.SaveChangesAsync();

            return calendarReminder;
        }

        public async Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder)
        {
            context.CalendarRemindersDb.Update(calendarReminder);
            await context.SaveChangesAsync();

            return calendarReminder;
        }

        public async Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder)
        {
            context.CalendarRemindersDb.Remove(calendarReminder);
            await context.SaveChangesAsync();

            return calendarReminder;
        }

        public async Task<List<CalendarReminder>> GetCalendarRemindersForUser(string userId)
        {
            List<CalendarReminder> calendarReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.UserId == userId).ToListAsync();

            return calendarReminders;
        }

        public async Task<List<CalendarReminder>> GetExpiredCalendarReminders()
        {
            List<CalendarReminder> expiredReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.NotifyTime < DateTime.UtcNow && !c.Notified).ToListAsync();

            return expiredReminders;
        }
    }
}
