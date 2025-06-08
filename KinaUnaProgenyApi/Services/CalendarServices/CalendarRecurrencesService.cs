using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public class CalendarRecurrencesService(ProgenyDbContext context) : ICalendarRecurrencesService
    {
        /// <summary>
        /// Gets a list of CalendarItems generated from recurring events for a Progeny.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="start">DateTime with the start date. Results include this day.</param>
        /// <param name="end">DateTime with the end date. Results include this day.</param>
        /// <param name="includeOriginal">Include the original event in the list.</param>
        /// <returns>List of CalendarItems</returns>
        public async Task<List<CalendarItem>> GetRecurringEventsForProgeny(int progenyId, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;

            end = new DateTime(end.Year, end.Month, end.Day, 23, 59, 59, DateTimeKind.Utc);
            foreach (RecurrenceRule recurrenceRule in recurrenceRules)
            {
                recurringEvents.AddRange(await GetCalendarItemsForRecurrenceRule(recurrenceRule, start, end, includeOriginal));
            }

            return recurringEvents;
        }

        public async Task<List<CalendarItem>> GetCalendarItemsForRecurrenceRule(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];
            if (recurrenceRule == null) return recurringEvents;

            // If the recurrence rule has a start date in the future, continue.
            if (recurrenceRule.Start.Date > end.Date)
            {
                return recurringEvents;
            }

            // If the recurrence rule has an end date before the start date, continue.
            if (recurrenceRule.EndOption == 1 && (recurrenceRule.Until.HasValue && recurrenceRule.Until.Value.Date < start.Date))
            {
                return recurringEvents;
            }

            recurringEvents = recurrenceRule.Frequency switch
            {
                1 => await GetDailyRecurrences(recurrenceRule, start, end, includeOriginal),
                2 => await GetWeeklyRecurrences(recurrenceRule, start, end, includeOriginal),
                3 => await GetMonthlyByDayRecurrences(recurrenceRule, start, end, includeOriginal),
                4 => await GetMonthlyByDateRecurrences(recurrenceRule, start, end, includeOriginal),
                5 => await GetYearlyByDayRecurrences(recurrenceRule, start, end, includeOriginal),
                6 => await GetYearlyByDateRecurrences(recurrenceRule, start, end, includeOriginal),
                _ => recurringEvents
            };

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetDailyRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = start.Date;
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = calendarItem.StartTime.Value.Date;
            }

            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    CalendarItem calendarItemToAdd = new();
                    calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                    if (calendarItemToAdd.StartTime == null || calendarItemToAdd.EndTime == null)
                    {
                        continue;
                    }

                    TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                    calendarItemToAdd.StartTime = new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                    calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                    if (recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount))
                    {
                        if (calendarItemToAdd.StartTime >= calendarItem.StartTime)
                        {
                            recurrenceInstancesCount++;

                            if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                            {
                                // Don't add original CalendarItem as it is already included.
                                if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                {
                                    recurringEvents.Add(calendarItemToAdd);
                                }
                            }
                        }
                    }
                }

                nextDate = nextDate.AddDays(recurrenceRule.Interval);
            }

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetWeeklyRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = start.Date;
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = calendarItem.StartTime.Value.Date;
            }
            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    List<string> weeklyDays = [.. recurrenceRule.ByDay.Split(',')];
                    for (int dayNumberOfWeek = 1; dayNumberOfWeek <= 7; dayNumberOfWeek++)
                    {
                        foreach (string weeklyDay in weeklyDays)
                        {
                            DateTime tempDate = nextDate.AddDays(dayNumberOfWeek);
                            if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDay)) continue;

                            CalendarItem calendarItemToAdd = new();
                            calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                            if (calendarItemToAdd.StartTime == null || calendarItemToAdd.EndTime == null) continue;
                            
                            TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                            calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                            calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                            if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                            if (!(calendarItemToAdd.StartTime > calendarItem.StartTime)) continue;
                            recurrenceInstancesCount++;

                            if (!(calendarItemToAdd.StartTime <= end) || !(calendarItemToAdd.StartTime >= start)) continue;

                            // Don't add original CalendarItem as it is already included.
                            if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                            {
                                recurringEvents.Add(calendarItemToAdd);
                            }
                        }
                    }
                }

                nextDate = nextDate.AddDays(recurrenceRule.Interval * 7);
            }

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetMonthlyByDayRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = new(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = new DateTime(calendarItem.StartTime.Value.Year, calendarItem.StartTime.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    // Get the day of the week and the week number from ByDay
                    List<string> weeklyDays = [.. recurrenceRule.ByDay.Split(',')];

                    for (int dayNumberOfMonth = 1; dayNumberOfMonth <= DateTime.DaysInMonth(nextDate.Year, nextDate.Month); dayNumberOfMonth++)
                    {
                        foreach (string weeklyDay in weeklyDays)
                        {
                            string weeklyDayNumber = weeklyDay[..^2];
                            string weeklyDayName = weeklyDay[^2..];

                            if (!int.TryParse(weeklyDayNumber, out int dayNumber)) continue;

                            DateTime tempDate = new(nextDate.Year, nextDate.Month, dayNumberOfMonth, 0, 0, 0, DateTimeKind.Utc);

                            if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName)) continue;

                            int weekNumber = (dayNumberOfMonth - 1) / 7 + 1;
                            if (weekNumber != dayNumber && dayNumber != -1) continue;

                            if (dayNumber is > 0 and < 6)
                            {
                                CalendarItem calendarItemToAdd = new();
                                calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                if (calendarItemToAdd.StartTime != null && calendarItemToAdd.EndTime != null)
                                {
                                    TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                    calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                                    calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                                    if (recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount))
                                    {
                                        if (calendarItemToAdd.StartTime > calendarItem.StartTime)
                                        {
                                            recurrenceInstancesCount++;

                                            if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                            {
                                                // Don't add original CalendarItem as it is already included.
                                                if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                                {
                                                    recurringEvents.Add(calendarItemToAdd);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (dayNumber != -1) continue;

                            // Last day of the month.
                            DateTime lastDayOfMonth = new(nextDate.Year, nextDate.Month, DateTime.DaysInMonth(nextDate.Year, nextDate.Month), 0, 0, 0, DateTimeKind.Utc);
                            for (int weekDayIndex = 0; weekDayIndex < 7; weekDayIndex++)
                            {
                                DateTime tempLastDayOfMonth = lastDayOfMonth.AddDays(-weekDayIndex);
                                if (tempLastDayOfMonth.Date != tempDate.Date) continue;
                                if (RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName) != (int)tempLastDayOfMonth.DayOfWeek) continue;

                                CalendarItem calendarItemToAdd = new();
                                calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                if (calendarItemToAdd.StartTime == null || calendarItemToAdd.EndTime == null) continue;

                                TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                                calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                if (!(calendarItemToAdd.StartTime > calendarItem.StartTime)) continue;
                                recurrenceInstancesCount++;

                                if (!(calendarItemToAdd.StartTime <= end) || !(calendarItemToAdd.StartTime >= start)) continue;

                                // Don't add original CalendarItem as it is already included.
                                if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                {
                                    recurringEvents.Add(calendarItemToAdd);
                                }
                            }
                        }
                    }
                }

                nextDate = nextDate.AddMonths(recurrenceRule.Interval);
            }

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetMonthlyByDateRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = recurrenceRule.Start.Date;
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = calendarItem.StartTime.Value.Date;
            }

            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    List<string> dayNumers = [.. recurrenceRule.ByMonthDay.Split(',')];
                    for (int i = 0; i < 31; i++)
                    {
                        if (!dayNumers.Contains(i.ToString())) continue;

                        DateTime tempDate = new(nextDate.Year, nextDate.Month, i, 0, 0, 0, DateTimeKind.Utc);
                        CalendarItem calendarItemToAdd = new();
                        calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                        if (calendarItemToAdd.StartTime != null && calendarItemToAdd.EndTime != null)
                        {
                            TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                            calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                            calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                            if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                            if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                            recurrenceInstancesCount++;

                            if (!(calendarItemToAdd.StartTime <= end) || !(calendarItemToAdd.StartTime >= start)) continue;

                            // Don't add original CalendarItem as it is already included.
                            if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                            {
                                recurringEvents.Add(calendarItemToAdd);
                            }
                        }
                    }
                }

                nextDate = nextDate.AddMonths(recurrenceRule.Interval);
            }

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetYearlyByDayRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = recurrenceRule.Start.Date;
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = calendarItem.StartTime.Value.Date;
            }
            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    // Get month from ByMonth
                    List<string> months = [.. recurrenceRule.ByMonth.Split(',')];
                    foreach (string byMonthItemAsString in months)
                    {
                        // if the byMonth value doesn't match the month of the nextDate, continue.
                        if (!int.TryParse(byMonthItemAsString, out int monthNumber)) continue;

                        for (int monthIndex = 0; monthIndex < 12; monthIndex++)
                        {
                            DateTime monthCheckDateTime = nextDate.AddMonths(monthIndex);
                            if (monthCheckDateTime.Month != monthNumber) continue;

                            // Get the day of the week and the week number from ByDay
                            List<string> weeklyDays = [.. recurrenceRule.ByDay.Split(',')];

                            for (int dayNumberOfMonth = 1; dayNumberOfMonth <= DateTime.DaysInMonth(monthCheckDateTime.Year, monthCheckDateTime.Month); dayNumberOfMonth++)
                            {
                                foreach (string weeklyDay in weeklyDays)
                                {
                                    string weeklyDayNumber = weeklyDay[..^2];
                                    string weeklyDayName = weeklyDay[^2..];

                                    if (!int.TryParse(weeklyDayNumber, out int dayNumber)) continue;

                                    DateTime tempDate = new(monthCheckDateTime.Year, monthCheckDateTime.Month, dayNumberOfMonth, 0, 0, 0, DateTimeKind.Utc);

                                    if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName)) continue;

                                    int weekNumber = (dayNumberOfMonth - 1) / 7 + 1;
                                    if (weekNumber != dayNumber && dayNumber != -1) continue;

                                    if (dayNumber is > 0 and < 6)
                                    {
                                        CalendarItem calendarItemToAdd = new();
                                        calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                        TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                        calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                                        calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                        if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                        if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                                        recurrenceInstancesCount++;

                                        if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                        {
                                            // Don't add original CalendarItem as it is already included.
                                            if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                            {
                                                recurringEvents.Add(calendarItemToAdd);
                                            }
                                        }
                                    }

                                    if (dayNumber != -1) continue;

                                    // Last day of the month.
                                    DateTime lastDayOfMonth = new(monthCheckDateTime.Year, monthCheckDateTime.Month, DateTime.DaysInMonth(monthCheckDateTime.Year, monthCheckDateTime.Month), 0, 0, 0, DateTimeKind.Utc);
                                    for (int weekDayIndex = 0; weekDayIndex < 7; weekDayIndex++)
                                    {
                                        DateTime tempLastDayOfMonth = lastDayOfMonth.AddDays(-weekDayIndex);
                                        if (tempLastDayOfMonth != tempDate) continue;
                                        if (RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName) != (int)tempLastDayOfMonth.DayOfWeek) continue;

                                        CalendarItem calendarItemToAdd = new();
                                        calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                        TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                        calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                                        calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                        if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                        if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                                        recurrenceInstancesCount++;

                                        if (!(calendarItemToAdd.StartTime <= end) || !(calendarItemToAdd.StartTime >= start)) continue;

                                        // Don't add original CalendarItem as it is already included.
                                        if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                        {
                                            recurringEvents.Add(calendarItemToAdd);
                                        }
                                    }

                                }

                            }
                        }
                    }
                }


                nextDate = nextDate.AddYears(recurrenceRule.Interval);
            }

            return recurringEvents;
        }

        private async Task<List<CalendarItem>> GetYearlyByDateRecurrences(RecurrenceRule recurrenceRule, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = [];

            CalendarItem calendarItem = await context.CalendarDb.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ProgenyId == recurrenceRule.ProgenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);

            if (!calendarItem.StartTime.HasValue)
            {
                return recurringEvents;
            }

            int recurrenceInstancesCount = 0;

            DateTime nextDate = recurrenceRule.Start.Date;
            if (recurrenceRule.EndOption == 2)
            {
                nextDate = calendarItem.StartTime.Value.Date;
            }
            while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
            {
                if (nextDate >= start || recurrenceRule.EndOption == 2)
                {
                    // Get month from ByMonth
                    List<string> months = [.. recurrenceRule.ByMonth.Split(',')];
                    foreach (string month in months)
                    {
                        // If the byMonth value doesn't match the month of the nextDate, continue.
                        if (!int.TryParse(month, out int monthNumber)) continue;

                        // Get the days of the month numbers from ByMonthDay
                        List<string> dayNumbers = [.. recurrenceRule.ByMonthDay.Split(',')];
                        for (int i = 0; i < 31; i++)
                        {
                            // If the day number doesn't match the day of the month, continue.
                            if (!dayNumbers.Contains(i.ToString())) continue;

                            DateTime tempDate = new(nextDate.Year, monthNumber, i, 0, 0, 0, DateTimeKind.Utc);
                            CalendarItem calendarItemToAdd = new();
                            calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                            if (calendarItemToAdd.StartTime != null && calendarItemToAdd.EndTime != null)
                            {
                                TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                                calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                recurrenceInstancesCount++;
                                if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;

                                if (!(calendarItemToAdd.StartTime <= end) || !(calendarItemToAdd.StartTime >= start)) continue;

                                // Don't add original CalendarItem as it is already included.
                                if (includeOriginal || calendarItemToAdd.StartTime != calendarItem.StartTime)
                                {
                                    recurringEvents.Add(calendarItemToAdd);
                                }
                            }
                        }
                    }
                }

                nextDate = nextDate.AddYears(recurrenceRule.Interval);
            }

            return recurringEvents;
        }
    }
}
