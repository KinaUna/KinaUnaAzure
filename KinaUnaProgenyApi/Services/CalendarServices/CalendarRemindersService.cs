using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
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

        public async Task<CustomResult<CalendarReminder>> GetCalendarReminder(int id, UserInfo userInfo)
        {
            CalendarReminder calendarReminder = await context.CalendarRemindersDb.AsNoTracking().FirstOrDefaultAsync(c => c.CalendarReminderId == id);

            if (calendarReminder == null) return CustomError.NotFoundError($"CalendarReminderService, GetCalendarReminder: CalendarReminder with id {id} not found.");

            if (calendarReminder.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, GetCalendarReminder: User is not authorized to access this CalendarReminder item.");
            }

            return calendarReminder;
        }

        public async Task<CustomResult<CalendarReminder>> AddCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo)
        {
            if (calendarReminder.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, AddCalendarReminder: User is not authorized to add this CalendarReminder item.");
            }

            CalendarReminder existingCalendarReminder = await context.CalendarRemindersDb.AsNoTracking().FirstOrDefaultAsync(c => c.CalendarReminderId == calendarReminder.CalendarReminderId);
            if (existingCalendarReminder != null)
            {
                return CustomError.ValidationError("CalendarReminderService, AddCalendarReminder: CalendarReminder already exists.");
            }
        
            context.CalendarRemindersDb.Add(calendarReminder);
            _ = await context.SaveChangesAsync();

            return calendarReminder;
        }

        public async Task<CustomResult<CalendarReminder>> UpdateCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo)
        {
            CalendarReminder existingCalendarReminder = await context.CalendarRemindersDb.AsNoTracking().FirstOrDefaultAsync(c => c.CalendarReminderId == calendarReminder.CalendarReminderId);
            if (existingCalendarReminder == null)
            {
                return CustomError.NotFoundError($"CalendarReminderService, UpdateCalendarReminder: CalendarReminder with id {calendarReminder.CalendarReminderId} not found.");
            }

            if (existingCalendarReminder.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, UpdateCalendarReminder: User is not authorized to update this CalendarReminder item.");
            }
            
            existingCalendarReminder.NotifyTime = calendarReminder.NotifyTime;
            existingCalendarReminder.Notified = calendarReminder.Notified;

            _ = context.CalendarRemindersDb.Update(existingCalendarReminder);
            _ = await context.SaveChangesAsync();

            return existingCalendarReminder;
        }

        public async Task<CustomResult<CalendarReminder>> DeleteCalendarReminder(CalendarReminder calendarReminder, UserInfo userInfo)
        {
            CalendarReminder existingCalendarReminder = await context.CalendarRemindersDb.AsNoTracking().FirstOrDefaultAsync(c => c.CalendarReminderId == calendarReminder.CalendarReminderId);
            if (existingCalendarReminder == null)
            {
                return CustomError.NotFoundError($"CalendarReminderService, DeleteCalendarReminder: CalendarReminder with id {calendarReminder.CalendarReminderId} not found.");
            }

            if (existingCalendarReminder.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, DeleteCalendarReminder: User is not authorized to delete this CalendarReminder item.");
            }
            
            context.CalendarRemindersDb.Remove(calendarReminder);
            await context.SaveChangesAsync();

            return calendarReminder;
        }

        public async Task<CustomResult<List<CalendarReminder>>> GetCalendarRemindersForUser(CalendarRemindersForUserRequest request, UserInfo userInfo)
        {
            if (request.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, GetCalendarRemindersForUser: User is not authorized to access this CalendarReminder item.");
            }

            List<CalendarReminder> calendarReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.UserId == request.UserId).ToListAsync();
            if (request.FilterNotified)
            {
                calendarReminders = calendarReminders.Where(c => !c.Notified).ToList();
            }

            return calendarReminders;
        }

        public async Task<List<CalendarReminder>> GetExpiredCalendarReminders()
        {
            List<CalendarReminder> expiredReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.NotifyTime < DateTime.UtcNow && !c.Notified).ToListAsync();

            return expiredReminders;
        }
    }
}
