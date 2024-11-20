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
        private readonly ICalendarRecurrencesService _calendarRecurrencesService;

        public CalendarService(ProgenyDbContext context, IDistributedCache cache, ICalendarRecurrencesService calendarRecurrencesService)
        {
            _context = context;
            _cache = cache;
            _calendarRecurrencesService = calendarRecurrencesService;
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
                item.RecurrenceRule.Start = item.StartTime ?? DateTime.UtcNow;
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
                item.RecurrenceRule.Start = item.StartTime ?? DateTime.UtcNow;
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

                // Check if there are any reminders with the recurrence rule, if so remove the recurrence reference.
                CalendarReminder reminder = await _context.CalendarRemindersDb.SingleOrDefaultAsync(cr => cr.EventId == item.EventId && cr.RecurrenceRuleId == calendarItemToUpdate.RecurrenceRuleId);
                if (reminder != null)
                {
                    reminder.RecurrenceRuleId = 0;
                    _ = _context.CalendarRemindersDb.Update(reminder);
                    _ = await _context.SaveChangesAsync();
                }
            }

            // If the item has a RecurrenceRule and the new item has a RecurrenceRule, update the RecurrenceRule in the database.
            if (calendarItemToUpdate.RecurrenceRuleId > 0 && item.RecurrenceRule.Frequency > 0)
            {
                RecurrenceRule recurrenceRule = await _context.RecurrenceRulesDb.SingleOrDefaultAsync(r => r.RecurrenceRuleId == calendarItemToUpdate.RecurrenceRuleId);
                recurrenceRule.Frequency = item.RecurrenceRule.Frequency;
                recurrenceRule.Interval = item.RecurrenceRule.Interval;
                recurrenceRule.Count = item.RecurrenceRule.Count;
                recurrenceRule.Start = item.StartTime ?? DateTime.UtcNow;
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

            // Update the reminders for the event.
            List<CalendarReminder> reminders = await _context.CalendarRemindersDb.Where(cr => cr.EventId == item.EventId).ToListAsync();
            foreach (CalendarReminder calendarReminder in reminders)
            {
                if (item.StartTime == null || calendarItemToUpdate.StartTime == null) continue;

                TimeSpan reminderOffSet = calendarReminder.NotifyTime - calendarItemToUpdate.StartTime.Value;
                calendarReminder.NotifyTime = item.StartTime.Value.Add(reminderOffSet);

                _ = _context.CalendarRemindersDb.Update(calendarReminder);
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

            // Check if there are any reminders for the event, if so remove them.
            //Todo: Notify users with reminders that the event has been removed.
            List<CalendarReminder> reminders = await _context.CalendarRemindersDb.Where(cr => cr.EventId == item.EventId).ToListAsync();
            foreach (CalendarReminder calendarReminder in reminders)
            {
                _ = _context.CalendarRemindersDb.Remove(calendarReminder);
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
                calendarList = calendarList.Where(c => c.StartTime >= start && c.StartTime <= end && c.AccessLevel >= accessLevel).ToList();
                List<CalendarItem> recurringEvents = await GetRecurringEventsForProgeny(progenyId, start.Value, end.Value, false);
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

        /// <summary>
        /// Gets a list of CalendarItems generated from recurring events for a Progeny.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="start">DateTime with the start date. Results include this day.</param>
        /// <param name="end">DateTime with the end date. Results include this day.</param>
        /// <param name="includeOriginal">Include the original event in the list.</param>
        /// <returns>List of CalendarItems</returns>
        private async Task<List<CalendarItem>> GetRecurringEventsForProgeny(int progenyId, DateTime start, DateTime end, bool includeOriginal)
        {
            List<CalendarItem> recurringEvents = await _calendarRecurrencesService.GetRecurringEventsForProgeny(progenyId, start, end, includeOriginal);
            return recurringEvents;
        }

        /// <summary>
        /// Gets the list of CalendarItems for a Progeny that are recurring events on this day.
        /// Only includes items after 1900.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny to get items for.</param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetRecurringCalendarItemsOnThisDay(int progenyId)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await _context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;
            DateTime today = DateTime.UtcNow.Date;
            
            for (int i = 1900; i < today.Year; i++)
            {
                DateTime onThisDayDateTime = new DateTime(i, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc);
                List<CalendarItem> itemsForYear = await GetRecurringEventsForProgeny(progenyId, onThisDayDateTime, onThisDayDateTime, false);
                if (itemsForYear.Count > 0)
                {
                    recurringEvents.AddRange(itemsForYear);
                }
            }

            return recurringEvents;
        }

        /// <summary>
        /// Gets the list of CalendarItems for a Progeny that are recurring events for the latest posts list.
        /// Only includes items after 1900.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny to get items for.</param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetRecurringCalendarItemsLatestPosts(int progenyId)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await _context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;
            DateTime start = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            recurringEvents = await GetRecurringEventsForProgeny(progenyId, start, DateTime.UtcNow.Date, false);

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
