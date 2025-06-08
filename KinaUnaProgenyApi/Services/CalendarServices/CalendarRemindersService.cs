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
    public class CalendarRemindersService(ProgenyDbContext context, IEmailSender emailSender, IPushMessageSender pushMessageSender, ICalendarRecurrencesService calendarRecurrencesService) : ICalendarRemindersService
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
            CalendarReminder existingCalendarReminder = await context.CalendarRemindersDb.FirstOrDefaultAsync(c => c.CalendarReminderId == calendarReminder.CalendarReminderId);
            if (existingCalendarReminder == null)
            {
                return CustomError.NotFoundError($"CalendarReminderService, UpdateCalendarReminder: CalendarReminder with id {calendarReminder.CalendarReminderId} not found.");
            }

            if (existingCalendarReminder.UserId != userInfo.UserId && !userInfo.IsKinaUnaAdmin)
            {
                return CustomError.UnauthorizedError("CalendarReminderService, UpdateCalendarReminder: User is not authorized to update this CalendarReminder item.");
            }
            
            existingCalendarReminder.NotifyTimeOffsetType = calendarReminder.NotifyTimeOffsetType;
            existingCalendarReminder.NotifyTime = calendarReminder.NotifyTime;
            existingCalendarReminder.Notified = calendarReminder.Notified;
            existingCalendarReminder.NotifiedDate = calendarReminder.NotifiedDate;
            
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
                calendarReminders = [.. calendarReminders.Where(c => !c.Notified)];
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

        /// <summary>
        /// Gets the list of reminders that are due to be sent/notified of.
        /// Does not include recurring events.
        /// </summary>
        /// <returns>List of CalendarReminder objects.</returns>
        public async Task SendExpiredCalendarReminders()
        {
            List<CalendarReminder> expiredReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.NotifyTime < DateTime.UtcNow && !c.Notified && c.RecurrenceRuleId > 0).ToListAsync();

            foreach (CalendarReminder calendarReminder in expiredReminders)
            {
                await SendCalendarReminder(calendarReminder.CalendarReminderId, null);
            }

        }

        public async Task SendExpiredRecurringReminders()
        {
            List<CalendarReminder> recurringReminders = await context.CalendarRemindersDb.AsNoTracking().Where(c => c.RecurrenceRuleId > 0).ToListAsync();
            foreach (CalendarReminder calendarReminder in recurringReminders)
            {
                CalendarItem calendarItem = await context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == calendarReminder.EventId);
                if (calendarItem == null) continue;
                if (!calendarItem.StartTime.HasValue || !calendarItem.EndTime.HasValue) continue;

                TimeSpan reminderOffset = calendarItem.StartTime.Value - calendarReminder.NotifyTime;
                
                RecurrenceRule recurrenceRule = await context.RecurrenceRulesDb.AsNoTracking().SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarReminder.RecurrenceRuleId);
                if (recurrenceRule == null) continue;

                DateTime startDateTime = DateTime.UtcNow - reminderOffset;
                DateTime endDateTime = DateTime.UtcNow + reminderOffset;
                if (reminderOffset < TimeSpan.Zero)
                {
                    startDateTime = DateTime.UtcNow + reminderOffset;
                    endDateTime = DateTime.UtcNow - reminderOffset;
                }

                List<CalendarItem> recurringEvents = await calendarRecurrencesService.GetCalendarItemsForRecurrenceRule(recurrenceRule, startDateTime, endDateTime, true);
                if (recurringEvents.Count <= 0) continue;

                foreach (CalendarItem recurringCalendarItem in recurringEvents)
                {
                    DateTime notificationTriggerTime = recurringCalendarItem.StartTime!.Value - reminderOffset;
                    TimeSpan timeSinceLastNotification = DateTime.UtcNow - calendarReminder.NotifiedDate;
                    // If the notificationTriggerTime is before now, but not more than 6 hours ago and the last notification was at least 24 hours ago, send a notification.
                    // We are assuming that recurring events are at least one day apart and that services are never interrupted for longer than 6 hours too.
                    if (notificationTriggerTime < DateTime.UtcNow && notificationTriggerTime > DateTime.UtcNow - TimeSpan.FromHours(6) && timeSinceLastNotification >= TimeSpan.FromHours(24))
                    {
                        await SendCalendarReminder(calendarReminder.CalendarReminderId, DateOnly.FromDateTime(recurringCalendarItem.StartTime!.Value.Date));
                    }
                }
            }
        }

        /// <summary>
        /// Sends a reminder for a calendar event.
        /// </summary>
        /// <param name="id">The Id of the Reminder entity</param>
        /// <param name="recurrenceDate">The event date for recurring events, null for non-recurring events.</param>
        /// <returns></returns>
        private async Task SendCalendarReminder(int id, DateOnly? recurrenceDate)
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

            // For recurring events, we need to adjust the start and end times to the correct date.
            if (recurrenceDate != null)
            {
                TimeSpan calendarItemDuration = calendarItem.EndTime.Value - calendarItem.StartTime.Value;
                calendarItem.StartTime = new DateTime(recurrenceDate.Value.Year, recurrenceDate.Value.Month, recurrenceDate.Value.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0,
                    DateTimeKind.Utc);

                calendarItem.EndTime = calendarItem.StartTime.Value + calendarItemDuration;
            }

            calendarItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(calendarItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(reminderUserInfo.Timezone));
            calendarItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(calendarItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(reminderUserInfo.Timezone));

            calendarItem.StartString = calendarItem.StartTime?.ToString("dd-MM-yyyy HH:mm") ?? "Error: Start time undefined."; // Todo: Localize/User defined format.
            calendarItem.EndString = calendarItem.EndTime?.ToString("dd-MM-yyyy HH:mm") ?? "Error: End time undefined."; // Todo: Localize/User defined format.
            

            Progeny calendarItemProgeny = await context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == calendarItem.ProgenyId);

            // Todo: Add EventDate parameter for popup with recurring events.
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
            calendarReminder.NotifiedDate = DateTime.UtcNow;

            await UpdateCalendarReminder(calendarReminder, reminderUserInfo);
        }
    }
}
