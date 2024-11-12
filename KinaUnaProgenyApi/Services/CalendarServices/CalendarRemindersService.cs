using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public class CalendarRemindersService(ProgenyDbContext context, IEmailSender emailSender, IPushMessageSender pushMessageSender ) : ICalendarRemindersService
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

        public async Task<CustomResult<List<CalendarReminder>>> GetUsersCalendarRemindersForEvent(int eventId, string reminderUserId, UserInfo currentUserInfo)
        {

            List<CalendarReminder> calendarReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.EventId == eventId && c.UserId == reminderUserId).ToListAsync();
            if (calendarReminders.Count < 1) return calendarReminders;

            if(currentUserInfo.UserId != calendarReminders[0].UserId && !currentUserInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, GetUsersCalendarRemindersForEvent: User is not authorized to access this CalendarReminder item.");
            }

            return calendarReminders;
        }

        public async Task<List<CalendarReminder>> GetExpiredCalendarReminders()
        {
            List<CalendarReminder> expiredReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.NotifyTime < DateTime.UtcNow && !c.Notified).ToListAsync();

            return expiredReminders;
        }

        public async Task SendCalendarReminder(int id)
        {
            CalendarReminder calendarReminder = await context.CalendarRemindersDb.AsNoTracking().SingleOrDefaultAsync(c => c.CalendarReminderId == id);
            if (calendarReminder == null) return;

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == calendarReminder.EventId);
            if (calendarItem == null) return;
            
            UserInfo reminderUserInfo = await context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == calendarReminder.UserId);
            if (reminderUserInfo == null) return;

            if (!calendarItem.StartTime.HasValue || !calendarItem.EndTime.HasValue)
            {
                return;
            }

            calendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(calendarItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(reminderUserInfo.Timezone));
            calendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(calendarItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(reminderUserInfo.Timezone));

            calendarItem.StartString = calendarItem.StartTime?.ToString("dd-MM-yyyy HH:mm") ?? "Error: Start time undefined."; // Todo: Localize/User defined format.
            calendarItem.EndString = calendarItem.EndTime?.ToString("dd-MM-yyyy HH:mm") ?? "Error: End time undefined."; // Todo: Localize/User defined format.
            

            Progeny calendarItemProgeny = await context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == calendarItem.ProgenyId);

            string eventLink = $"{Constants.WebAppUrl}/Calendar?eventId={calendarItem.EventId}&childId={calendarItemProgeny.Id}";

            string reminderTitle = $"{calendarItemProgeny.NickName}: {calendarItem.Title} - KinaUna Reminder"; // Todo: Localize.
            string reminderBody = $"<div>Event reminder for: {calendarItemProgeny.NickName}</div>"; // Todo: Localize and use template.
            reminderBody += $"<div>Title: {calendarItem.Title}</div>";
            reminderBody += $"<div>Start: {calendarItem.StartString}</div>"; // Todo: Localize and use template.
            reminderBody += $"<div>End: {calendarItem.EndString}</div>";
            reminderBody += $"<div>Location: {calendarItem.Location}</div>";
            reminderBody += $"<div>Context: {calendarItem.Context}</div>";
            reminderBody += $"<div>Notes: {calendarItem.Notes}</div>";
            reminderBody += $"<div>Link: <a href=\"{eventLink}\">{calendarItemProgeny.NickName} : Calendar</a></div>";

            await emailSender.SendEmailAsync(reminderUserInfo.UserEmail, reminderTitle, reminderBody);

            await pushMessageSender.SendMessage(reminderUserInfo.UserId, reminderTitle, $"Event reminder for: {calendarItemProgeny.NickName}", eventLink, "kinaunacalendar" + calendarItem.EventId);

            calendarReminder.Notified = true;

            await UpdateCalendarReminder(calendarReminder, reminderUserInfo);
        }
    }
}
