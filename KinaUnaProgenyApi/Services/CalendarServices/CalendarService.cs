using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public class CalendarService : ICalendarService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public CalendarService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(1, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Get a CalendarItem from the cache.
        /// If it isn't in the cache gets it from the database.
        /// If it doesn't exist in the database, returns null.
        /// </summary>
        /// <param name="id">The CalendarItem's EventId</param>
        /// <returns>CalendarItem if it exists, null if it doesn't exist.</returns>
        public async Task<CalendarItem> GetCalendarItem(int id)
        {
            CalendarItem calendarItem = await GetCalendarItemFromCache(id);
            if (calendarItem == null || calendarItem.EventId == 0)
            {
                calendarItem = await SetCalendarItemInCache(id);
            }

            return calendarItem;
        }

        /// <summary>
        /// Add a new CalendarItem to the database.
        /// Sets the CalendarItem in the cache.
        /// </summary>
        /// <param name="item">The CalendarItem to add.</param>
        /// <returns>The added CalendarItem.</returns>
        public async Task<CalendarItem> AddCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToAdd = new();
            calendarItemToAdd.CopyPropertiesForAdd(item);

            if (item.RecurrenceRule.Frequency != 0)
            {
                item.RecurrenceRule.ProgenyId = calendarItemToAdd.ProgenyId;
                item.RecurrenceRule.EnsureStringsAreNotNull();

                _ = _context.RecurrenceRulesDb.Add(item.RecurrenceRule);
                _ = await _context.SaveChangesAsync();

                calendarItemToAdd.RecurrenceRuleId = item.RecurrenceRule.RecurrenceRuleId;
            }

            if (string.IsNullOrWhiteSpace(calendarItemToAdd.UId))
            {
                calendarItemToAdd.UId = Guid.NewGuid().ToString();
            }
            _ = _context.CalendarDb.Add(calendarItemToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetCalendarItemInCache(calendarItemToAdd.EventId);

            return calendarItemToAdd;
        }

        /// <summary>
        /// Get a CalendarItem from the cache.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to get.</param>
        /// <returns>The CalendarItem with the given EventId. If it doesn't exist, returns null.</returns>
        private async Task<CalendarItem> GetCalendarItemFromCache(int id)
        {
            CalendarItem calendarItem = new();
            string cachedCalendarItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }

            return calendarItem;
        }

        /// <summary>
        /// Sets a CalendarItem in the cache and updates the cached List of CalendarItems for the Progeny of this item.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem.</param>
        /// <returns>The CalendarItem with the given EventId. Null if the CalendarItem doesn't exist in the database.</returns>
        private async Task<CalendarItem> SetCalendarItemInCache(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            if (calendarItem == null) return null;

            if (calendarItem.RecurrenceRuleId > 0)
            {
                calendarItem.RecurrenceRule = await _context.RecurrenceRulesDb.AsNoTracking().SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarItem.RecurrenceRuleId);
            }

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);

            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + calendarItem.ProgenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);

            return calendarItem;
        }

        /// <summary>
        /// Removes a CalendarItem from the cache and updates the cached List of CalendarItems for the Progeny of this item.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the CalendarItem belongs to.</param>
        /// <returns></returns>
        private async Task RemoveCalendarItemFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);

            List<CalendarItem> calendarList = [.. _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId)];
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        /// <summary>
        /// Updates a CalendarItem in the database and sets the updated item in the cache.
        /// </summary>
        /// <param name="item">The CalendarItem with the updated properties.</param>
        /// <returns>The updated CalendarItem.</returns>
        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToUpdate = await _context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == item.EventId);
            if (calendarItemToUpdate == null) return null;

            // If the item has a RecurrenceRule, add it to the database if it doesn't exist.
            if (calendarItemToUpdate.RecurrenceRuleId == 0 && item.RecurrenceRule.Frequency != 0)
            {
                item.RecurrenceRule.ProgenyId = calendarItemToUpdate.ProgenyId;
                item.RecurrenceRule.EnsureStringsAreNotNull();

                _ = _context.RecurrenceRulesDb.Add(item.RecurrenceRule);
                _ = await _context.SaveChangesAsync();
                item.RecurrenceRuleId = item.RecurrenceRule.RecurrenceRuleId;
            }

            // If the item has a RecurrenceRule and the new item doesn't, remove the RecurrenceRule from the database.
            if (calendarItemToUpdate.RecurrenceRuleId > 0 && item.RecurrenceRule.Frequency == 0)
            {
                RecurrenceRule recurrenceRule = await _context.RecurrenceRulesDb.SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarItemToUpdate.RecurrenceRuleId);
                _ = _context.RecurrenceRulesDb.Remove(recurrenceRule);
                _ = await _context.SaveChangesAsync();
                item.RecurrenceRuleId = 0;
            }

            if (calendarItemToUpdate.RecurrenceRuleId > 0 && item.RecurrenceRule.Frequency > 0)
            {
                RecurrenceRule recurrenceRule = await _context.RecurrenceRulesDb.SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarItemToUpdate.RecurrenceRuleId);
                recurrenceRule.Frequency = item.RecurrenceRule.Frequency;
                recurrenceRule.Interval = item.RecurrenceRule.Interval;
                recurrenceRule.Count = item.RecurrenceRule.Count;
                recurrenceRule.Start = item.RecurrenceRule.Start;
                recurrenceRule.Until = item.RecurrenceRule.Until;
                recurrenceRule.ByDay = item.RecurrenceRule.ByDay;
                recurrenceRule.ByMonthDay = item.RecurrenceRule.ByMonthDay;
                recurrenceRule.ByMonth = item.RecurrenceRule.ByMonth;
                recurrenceRule.EndOption = item.RecurrenceRule.EndOption;
                recurrenceRule.ProgenyId = calendarItemToUpdate.ProgenyId;

                recurrenceRule.EnsureStringsAreNotNull();

                _ = _context.RecurrenceRulesDb.Update(recurrenceRule);
                _ = await _context.SaveChangesAsync();
            }


            calendarItemToUpdate.CopyPropertiesForUpdate(item);

            if (string.IsNullOrWhiteSpace(calendarItemToUpdate.UId))
            {
                calendarItemToUpdate.UId = Guid.NewGuid().ToString();
            }
            _ = _context.CalendarDb.Update(calendarItemToUpdate);
            _ = await _context.SaveChangesAsync();
            _ = await SetCalendarItemInCache(calendarItemToUpdate.EventId);

            return calendarItemToUpdate;
        }

        /// <summary>
        /// Deletes a CalendarItem from the database and removes it from the cache.
        /// </summary>
        /// <param name="item">The CalendarItem to delete.</param>
        /// <returns>The deleted CalendarItem.</returns>
        public async Task<CalendarItem> DeleteCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToDelete = await _context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == item.EventId);
            if (calendarItemToDelete == null) return null;

            if (calendarItemToDelete.RecurrenceRuleId > 0)
            {
                RecurrenceRule recurrenceRule = await _context.RecurrenceRulesDb.SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarItemToDelete.RecurrenceRuleId);
                _ = _context.RecurrenceRulesDb.Remove(recurrenceRule);
                _ = await _context.SaveChangesAsync();
            }

            _ = _context.CalendarDb.Remove(calendarItemToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveCalendarItemFromCache(item.EventId, item.ProgenyId);

            return item;
        }

        /// <summary>
        /// Gets a List of all CalendarItems for a Progeny from the cache.
        /// If the list isn't found in the cache, gets it from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all CalendarItems for.</param>
        /// <param name="accessLevel">The required access level to view the event.</param>
        /// <param name="start">Optional start date for the list.</param>
        /// <param name="end">Optional end date for the list.</param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel, DateTime? start = null, DateTime? end = null)
        {
            List<CalendarItem> calendarList = await GetCalendarListFromCache(progenyId);
            if (calendarList == null || calendarList.Count == 0)
            {
                calendarList = await SetCalendarListInCache(progenyId);
            }

            if (start != null && end != null)
            {
                calendarList = calendarList.Where(c => c.StartTime >= start && c.StartTime <= end && c.AccessLevel >= accessLevel && c.RecurrenceRuleId == 0).ToList();
                List<CalendarItem> recurringEvents = await GetRecurringEventsForProgeny(progenyId, start.Value, end.Value);
                calendarList.AddRange(recurringEvents);
            }
            else
            {
                calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();
            }

            return calendarList;
        }

        /// <summary>
        /// Gets a List of all CalendarItems for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all CalendarItems for.</param>
        /// <returns>List of CalendarItems.</returns>
        private async Task<List<CalendarItem>> GetCalendarListFromCache(int progenyId)
        {
            List<CalendarItem> calendarList = [];
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }

            return calendarList;
        }

        /// <summary>
        /// Sets a List of CalendarItems for a Progeny in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny.</param>
        /// <returns>List of all CalendarItems for the Progeny.</returns>
        private async Task<List<CalendarItem>> SetCalendarListInCache(int progenyId)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);

            return calendarList;
        }
        
        private async Task<List<CalendarItem>> GetRecurringEventsForProgeny(int progenyId, DateTime start, DateTime end)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await _context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;

            foreach (RecurrenceRule recurrenceRule in recurrenceRules)
            {
                CalendarItem calendarItem = await _context.CalendarDb.AsNoTracking().FirstOrDefaultAsync(c => c.ProgenyId == progenyId && c.RecurrenceRuleId == recurrenceRule.RecurrenceRuleId);
                if (!calendarItem.StartTime.HasValue)
                {
                    continue;
                }

                int recurrenceInstancesCount = 0;

                if (recurrenceRule.Frequency == 1)
                {
                    // Daily
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
                            TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                            calendarItemToAdd.StartTime = new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                            calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                            if (recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount))
                            {
                                if (calendarItemToAdd.StartTime > calendarItem.StartTime)
                                {
                                    recurrenceInstancesCount++;

                                    if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                    {
                                        recurringEvents.Add(calendarItemToAdd);
                                    }
                                }
                            }
                        }

                        nextDate = nextDate.AddDays(recurrenceRule.Interval);
                    }
                }
                else if (recurrenceRule.Frequency == 2)
                {
                    // Weekly
                    DateTime nextDate = start.Date;
                    if (recurrenceRule.EndOption == 2)
                    {
                        nextDate = calendarItem.StartTime.Value.Date;
                    }
                    while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
                    {
                        if (nextDate >= start || recurrenceRule.EndOption == 2)
                        {
                            List<string> weeklyDays = recurrenceRule.ByDay.Split(',').ToList();
                            for (int dayNumberOfWeek = 1; dayNumberOfWeek <= 7; dayNumberOfWeek++)
                            {
                                foreach (string weeklyDay in weeklyDays)
                                {
                                    DateTime tempDate = nextDate.AddDays(dayNumberOfWeek);
                                    if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDay)) continue;

                                    CalendarItem calendarItemToAdd = new();
                                    calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                    TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                    calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                    calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                                    if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                    if (!(calendarItemToAdd.StartTime > calendarItem.StartTime)) continue;
                                    recurrenceInstancesCount++;

                                    if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                    {
                                        recurringEvents.Add(calendarItemToAdd);

                                    }
                                }
                            }
                        }

                        nextDate = nextDate.AddDays(recurrenceRule.Interval * 7);
                    }
                }
                else if (recurrenceRule.Frequency == 3)
                {
                    // Monthly by day
                    DateTime nextDate = new DateTime(start.Year, start.Month, 1);
                    if (recurrenceRule.EndOption == 2)
                    {
                        nextDate = new DateTime(calendarItem.StartTime.Value.Year, calendarItem.StartTime.Value.Month, 1);
                    }

                    while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
                    {
                        if (nextDate >= start || recurrenceRule.EndOption == 2)
                        {
                            // Get the day of the week and the week number from ByDay
                            List<string> weeklyDays = recurrenceRule.ByDay.Split(',').ToList();

                            for (int dayNumberOfMonth = 1; dayNumberOfMonth <= DateTime.DaysInMonth(nextDate.Year, nextDate.Month); dayNumberOfMonth++)
                            {
                                foreach (string weeklyDay in weeklyDays)
                                {
                                    string weeklyDayNumber = weeklyDay.Substring(0, weeklyDay.Length - 2);
                                    string weeklyDayName = weeklyDay.Substring(weeklyDay.Length - 2);

                                    if (!int.TryParse(weeklyDayNumber, out int dayNumber)) continue;

                                    DateTime tempDate = new DateTime(nextDate.Year, nextDate.Month, dayNumberOfMonth);

                                    if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName)) continue;

                                    int weekNumber = (dayNumberOfMonth - 1) / 7 + 1;
                                    if (weekNumber != dayNumber && dayNumber != -1) continue;

                                    if (dayNumber is > 0 and < 6)
                                    {
                                        CalendarItem calendarItemToAdd = new();
                                        calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                        TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                        calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                        calendarItemToAdd.EndTime = calendarItemToAdd.StartTime.Value + calendarItemToAddDuration;

                                        if (recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount))
                                        {
                                            if (calendarItemToAdd.StartTime > calendarItem.StartTime)
                                            {
                                                recurrenceInstancesCount++;

                                                if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                                {
                                                    recurringEvents.Add(calendarItemToAdd);
                                                }
                                            }
                                        }
                                    }

                                    if (dayNumber != -1) continue;

                                    // Last day of the month.
                                    DateTime lastDayOfMonth = new DateTime(nextDate.Year, nextDate.Month, DateTime.DaysInMonth(nextDate.Year, nextDate.Month));
                                    for (int weekDayIndex = 0; weekDayIndex < 7; weekDayIndex++)
                                    {
                                        DateTime tempLastDayOfMonth = lastDayOfMonth.AddDays(-weekDayIndex);
                                        if (tempLastDayOfMonth != tempDate) continue;
                                        if (RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName) != (int)tempLastDayOfMonth.DayOfWeek) continue;

                                        CalendarItem calendarItemToAdd = new();
                                        calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                        TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                        calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                        calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                        if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                        if (!(calendarItemToAdd.StartTime > calendarItem.StartTime)) continue;
                                        recurrenceInstancesCount++;

                                        if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                        {
                                            recurringEvents.Add(calendarItemToAdd);
                                        }
                                    }
                                }
                            }
                        }

                        nextDate = nextDate.AddMonths(recurrenceRule.Interval);
                    }
                }
                else if (recurrenceRule.Frequency == 4)
                {
                    // Monthly by date
                    DateTime nextDate = recurrenceRule.Start.Date;
                    if (recurrenceRule.EndOption == 2)
                    {
                        nextDate = calendarItem.StartTime.Value.Date;
                    }
                    while (nextDate <= end && recurrenceRule.IsBeforeEnd(nextDate, recurrenceInstancesCount))
                    {
                        if (nextDate >= start || recurrenceRule.EndOption == 2)
                        {
                            List<string> dayNumers = recurrenceRule.ByMonthDay.Split(',').ToList();
                            for (int i = 0; i < 31; i++)
                            {
                                if (!dayNumers.Contains(i.ToString())) continue;
                                
                                DateTime tempDate = new DateTime(nextDate.Year, nextDate.Month, i);
                                CalendarItem calendarItemToAdd = new();
                                calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                                recurrenceInstancesCount++;
                                
                                if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                {
                                    recurringEvents.Add(calendarItemToAdd);
                                }
                            }
                        }

                        nextDate = nextDate.AddMonths(recurrenceRule.Interval);
                    }
                }
                else if (recurrenceRule.Frequency == 5)
                {
                    // Yearly by day
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
                            List<string> months = recurrenceRule.ByMonth.Split(',').ToList();
                            foreach (string byMonthItemAsString in months)
                            {
                                // if the byMonth value doesn't match the month of the nextDate, continue.
                                if (!int.TryParse(byMonthItemAsString, out int monthNumber)) continue;

                                for (int monthIndex = 0; monthIndex < 12; monthIndex++)
                                {
                                    DateTime monthCheckDateTime = nextDate.AddMonths(monthIndex);
                                    if (monthCheckDateTime.Month != monthNumber) continue;
                                    
                                    // Get the day of the week and the week number from ByDay
                                    List<string> weeklyDays = recurrenceRule.ByDay.Split(',').ToList();

                                    for (int dayNumberOfMonth = 1; dayNumberOfMonth <= DateTime.DaysInMonth(monthCheckDateTime.Year, monthCheckDateTime.Month); dayNumberOfMonth++)
                                    {
                                        foreach (string weeklyDay in weeklyDays)
                                        {
                                            string weeklyDayNumber = weeklyDay.Substring(0, weeklyDay.Length - 2);
                                            string weeklyDayName = weeklyDay.Substring(weeklyDay.Length - 2);

                                            if (!int.TryParse(weeklyDayNumber, out int dayNumber)) continue;

                                            DateTime tempDate = new DateTime(monthCheckDateTime.Year, monthCheckDateTime.Month, dayNumberOfMonth);

                                            if ((int)tempDate.DayOfWeek != RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName)) continue;

                                            int weekNumber = (dayNumberOfMonth - 1) / 7 + 1;
                                            if (weekNumber != dayNumber && dayNumber != -1) continue;

                                            if (dayNumber is > 0 and < 6)
                                            {
                                                CalendarItem calendarItemToAdd = new();
                                                calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                                TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                                calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                                calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                                if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                                if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                                                recurrenceInstancesCount++;

                                                if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                                {
                                                    recurringEvents.Add(calendarItemToAdd);
                                                }
                                            }

                                            if (dayNumber != -1) continue;

                                            // Last day of the month.
                                            DateTime lastDayOfMonth = new DateTime(monthCheckDateTime.Year, monthCheckDateTime.Month, DateTime.DaysInMonth(monthCheckDateTime.Year, monthCheckDateTime.Month));
                                            for (int weekDayIndex = 0; weekDayIndex < 7; weekDayIndex++)
                                            {
                                                DateTime tempLastDayOfMonth = lastDayOfMonth.AddDays(-weekDayIndex);
                                                if (tempLastDayOfMonth != tempDate) continue;
                                                if (RecurrenceUnits.WeeklyDays.IndexOf(weeklyDayName) != (int)tempLastDayOfMonth.DayOfWeek) continue;
                                                
                                                CalendarItem calendarItemToAdd = new();
                                                calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                                TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                                calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                                calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                                if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                                if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;
                                                recurrenceInstancesCount++;

                                                if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
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
                    
                }
                else if (recurrenceRule.Frequency == 6)
                {
                    // Yearly by date
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
                            List<string> months = recurrenceRule.ByMonth.Split(',').ToList();
                            foreach (string month in months)
                            {
                                // If the byMonth value doesn't match the month of the nextDate, continue.
                                if (!int.TryParse(month, out int monthNumber)) continue;

                                // Get the days of the month numbers from ByMonthDay
                                List<string> dayNumbers = recurrenceRule.ByMonthDay.Split(',').ToList();
                                for (int i = 0; i < 31; i++)
                                {
                                    // If the day number doesn't match the day of the month, continue.
                                    if (!dayNumbers.Contains(i.ToString())) continue;
                                    
                                    DateTime tempDate = new DateTime(nextDate.Year, monthNumber, i);
                                    CalendarItem calendarItemToAdd = new();
                                    calendarItemToAdd.CopyPropertiesForRecurringEvent(calendarItem);
                                    TimeSpan calendarItemToAddDuration = calendarItemToAdd.EndTime.Value - calendarItemToAdd.StartTime.Value;
                                    calendarItemToAdd.StartTime = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, calendarItem.StartTime.Value.Hour, calendarItem.StartTime.Value.Minute, 0);
                                    calendarItemToAdd.EndTime = calendarItemToAdd.StartTime + calendarItemToAddDuration;

                                    recurrenceInstancesCount++;
                                    if (!recurrenceRule.IsBeforeEnd(calendarItemToAdd.StartTime.Value, recurrenceInstancesCount)) continue;
                                    if (calendarItemToAdd.StartTime < calendarItem.StartTime) continue;

                                    if (calendarItemToAdd.StartTime <= end && calendarItemToAdd.StartTime >= start)
                                    {
                                        recurringEvents.Add(calendarItemToAdd);
                                        
                                    }
                                }
                            }
                        }

                        nextDate = nextDate.AddYears(recurrenceRule.Interval);
                    }

                }
            }

            return recurringEvents;
        }

        public async Task<List<CalendarItem>> GetCalendarItemsWithContext(int progenyId, string context, int accessLevel)
        {
            List<CalendarItem> allItems = await GetCalendarList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(context))
            {
                allItems = [.. allItems.Where(c => c.Context != null && c.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }

        /// <summary>
        /// Assigns UIds to all CalendarItems that don't have one.
        /// </summary>
        /// <returns></returns>
        public async Task CheckCalendarItemsForUId()
        {
            List<CalendarItem> allItems = await _context.CalendarDb.Where(c => string.IsNullOrWhiteSpace(c.UId)).ToListAsync();
            if (allItems.Any())
            {
                foreach (CalendarItem calendarItem in allItems)
                {
                    calendarItem.UId = Guid.NewGuid().ToString();
                    _ = _context.CalendarDb.Update(calendarItem);
                }

                _ = await _context.SaveChangesAsync();
            }
        }
    }
}
