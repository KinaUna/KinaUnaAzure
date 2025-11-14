using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.CalendarServices
{
    public class CalendarService : ICalendarService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        private readonly ICalendarRecurrencesService _calendarRecurrencesService;
        private readonly IAccessManagementService _accessManagementService;

        public CalendarService(ProgenyDbContext context, IDistributedCache cache, ICalendarRecurrencesService calendarRecurrencesService, IAccessManagementService accessManagementService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
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
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>CalendarItem if it exists, null if it doesn't exist.</returns>
        public async Task<CalendarItem> GetCalendarItem(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, id, currentUserInfo, PermissionLevel.View))
            {
                return new CalendarItem();
            }

            CalendarItem calendarItem = await GetCalendarItemFromCache(id);
            if (calendarItem == null || calendarItem.EventId == 0)
            {
                calendarItem = await SetCalendarItemInCache(id);
            }

            calendarItem.ItemPerMission =
                await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, calendarItem.EventId, calendarItem.ProgenyId, calendarItem.FamilyId, currentUserInfo);
            return calendarItem;
        }

        /// <summary>
        /// Add a new CalendarItem to the database.
        /// Sets the CalendarItem in the cache.
        /// </summary>
        /// <param name="item">The CalendarItem to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The added CalendarItem.</returns>
        public async Task<CalendarItem> AddCalendarItem(CalendarItem item, UserInfo currentUserInfo)
        {
            // A CalendarItem must belong to either a Progeny or a Family, not both.
            if (item.ProgenyId > 0 && item.FamilyId > 0)
            {
                return null;
            }

            bool hasAccess = false;
            if (item.ProgenyId > 0)
            {
                if (await _accessManagementService.HasProgenyPermission(item.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (item.FamilyId > 0)
            {
                if (await _accessManagementService.HasFamilyPermission(item.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }
            
            if (!hasAccess)
            {
                return null;
            }

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

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Calendar, calendarItemToAdd.EventId, calendarItemToAdd.ProgenyId, calendarItemToAdd.FamilyId, calendarItemToAdd.ItemPermissionsDtoList,
                currentUserInfo);

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
                calendarItem = JsonSerializer.Deserialize<CalendarItem>(cachedCalendarItem, JsonSerializerOptions.Web);
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

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, 
                JsonSerializer.Serialize(calendarItem, JsonSerializerOptions.Web), _cacheOptionsSliding);

            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId && c.FamilyId == calendarItem.FamilyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + calendarItem.ProgenyId + "_family_" + calendarItem.FamilyId,
                JsonSerializer.Serialize(calendarList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return calendarItem;
        }

        /// <summary>
        /// Removes a CalendarItem from the cache and updates the cached List of CalendarItems for the Progeny of this item.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the CalendarItem belongs to.</param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        private async Task RemoveCalendarItemFromCache(int id, int progenyId, int familyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);

            List<CalendarItem> calendarList = [.. _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId && c.FamilyId == familyId)];
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId + "_family_" + familyId, JsonSerializer.Serialize(calendarList, JsonSerializerOptions.Web), _cacheOptionsSliding);
        }

        /// <summary>
        /// Updates a CalendarItem in the database and sets the updated item in the cache.
        /// </summary>
        /// <param name="item">The CalendarItem with the updated properties.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The updated CalendarItem.</returns>
        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem item, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, item.EventId, currentUserInfo, PermissionLevel.Edit))
            {
                return new CalendarItem();
            }

            CalendarItem calendarItemToUpdate = await _context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == item.EventId);
            if (calendarItemToUpdate == null || calendarItemToUpdate.ProgenyId != item.ProgenyId || calendarItemToUpdate.FamilyId != item.FamilyId) return null;

            // If the item has a RecurrenceRule, add it to the database if it doesn't exist.
            if (calendarItemToUpdate.RecurrenceRuleId == 0 && item.RecurrenceRule.Frequency != 0)
            {
                item.RecurrenceRule.ProgenyId = calendarItemToUpdate.ProgenyId;
                item.RecurrenceRule.FamilyId = calendarItemToUpdate.FamilyId;
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
                recurrenceRule.FamilyId = calendarItemToUpdate.FamilyId;

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
            
            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Calendar, calendarItemToUpdate.EventId, calendarItemToUpdate.ProgenyId, calendarItemToUpdate.FamilyId, calendarItemToUpdate.ItemPermissionsDtoList,
                currentUserInfo);

            _ = await SetCalendarItemInCache(calendarItemToUpdate.EventId);

            return calendarItemToUpdate;
        }

        /// <summary>
        /// Deletes a CalendarItem from the database and removes it from the cache.
        /// </summary>
        /// <param name="item">The CalendarItem to delete.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The deleted CalendarItem.</returns>
        public async Task<CalendarItem> DeleteCalendarItem(CalendarItem item, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, item.EventId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

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

            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Calendar, calendarItemToDelete.EventId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            { 
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await RemoveCalendarItemFromCache(item.EventId, item.ProgenyId, item.FamilyId);

            return item;
        }

        /// <summary>
        /// Gets a List of all CalendarItems for a Progeny from the cache.
        /// If the list isn't found in the cache, gets it from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all CalendarItems for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="start">Optional start date for the list.</param>
        /// <param name="end">Optional end date for the list.</param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int familyId, UserInfo currentUserInfo, DateTime? start = null, DateTime? end = null)
        {
            if (progenyId == 0 && familyId == 0)
            {
                return [];
            }

            List<CalendarItem> calendarList = await GetCalendarListFromCache(progenyId, familyId);
            if (calendarList == null || calendarList.Count == 0)
            {
                calendarList = await SetCalendarListInCache(progenyId, familyId);
            }

            if (start != null && end != null)
            {
                calendarList = [.. calendarList.Where(c => c.StartTime >= start && c.StartTime <= end)];
                List<CalendarItem> recurringEvents = await GetRecurringEventsForProgenyOrFamily(progenyId, familyId, start.Value, end.Value, false, currentUserInfo);
                calendarList.AddRange(recurringEvents);
            }
            
            List<CalendarItem> accessibleCalendarItems = [];
            foreach (CalendarItem calendarItem in calendarList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, calendarItem.EventId, currentUserInfo, PermissionLevel.View))
                {
                    accessibleCalendarItems.Add(calendarItem);
                }
            }
            
            return accessibleCalendarItems;

        }

        /// <summary>
        /// Gets a List of all CalendarItems for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all CalendarItems for.</param>
        /// <param name="familyId">The FamilyId of the Family to get all CalendarItems for.</param>
        /// <returns>List of CalendarItems.</returns>
        private async Task<List<CalendarItem>> GetCalendarListFromCache(int progenyId, int familyId)
        {
            List<CalendarItem> calendarList = [];
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId + "_family_" + familyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonSerializer.Deserialize<List<CalendarItem>>(cachedCalendar, JsonSerializerOptions.Web);
            }

            return calendarList;
        }

        /// <summary>
        /// Sets a List of CalendarItems for a Progeny in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny.</param>
        /// <param name="familyId">The FamilyId of the Family.</param>
        /// <returns>List of all CalendarItems for the Progeny.</returns>
        private async Task<List<CalendarItem>> SetCalendarListInCache(int progenyId, int familyId)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId && c.FamilyId == familyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId + "_family_" + familyId, JsonSerializer.Serialize(calendarList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return calendarList;
        }

        /// <summary>
        /// Gets a list of CalendarItems generated from recurring events for a Progeny.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="familyId"></param>
        /// <param name="start">DateTime with the start date. Results include this day.</param>
        /// <param name="end">DateTime with the end date. Results include this day.</param>
        /// <param name="includeOriginal">Include the original event in the list.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>List of CalendarItems</returns>
        private async Task<List<CalendarItem>> GetRecurringEventsForProgenyOrFamily(int progenyId, int familyId, DateTime start, DateTime end, bool includeOriginal, UserInfo currentUserInfo)
        {
            List<CalendarItem> recurringEvents = await _calendarRecurrencesService.GetRecurringEventsForProgenyOrFamily(progenyId, familyId, start, end, includeOriginal, currentUserInfo);
            return recurringEvents;
        }

        /// <summary>
        /// Gets the list of CalendarItems for a Progeny that are recurring events on this day.
        /// Only includes items after 1900.
        /// </summary>
        /// <param name="progenyId">The id of the Progeny to get items for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetRecurringCalendarItemsOnThisDay(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await _context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId && r.FamilyId == familyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;
            DateTime today = DateTime.UtcNow.Date;
            
            for (int i = 1900; i < today.Year; i++)
            {
                DateTime onThisDayDateTime = new(i, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc);
                List<CalendarItem> itemsForYear = await GetRecurringEventsForProgenyOrFamily(progenyId, familyId, onThisDayDateTime, onThisDayDateTime, false, currentUserInfo);
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
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo"></param>
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetRecurringCalendarItemsLatestPosts(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            List<CalendarItem> recurringEvents = [];
            List<RecurrenceRule> recurrenceRules = await _context.RecurrenceRulesDb.AsNoTracking().Where(r => r.ProgenyId == progenyId && r.FamilyId == familyId).ToListAsync();
            if (recurrenceRules.Count == 0) return recurringEvents;
            DateTime start = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            recurringEvents = await GetRecurringEventsForProgenyOrFamily(progenyId, familyId, start, DateTime.UtcNow.Date, false, currentUserInfo);

            return recurringEvents;
        }

        /// <summary>
        /// Retrieves a list of calendar items for the specified progeny or family, filtered by context.
        /// </summary>
        /// <remarks>This method retrieves all calendar items associated with the specified progeny and
        /// family, and applies an optional  filter based on the provided context. The context filter is
        /// case-insensitive and matches substrings within the  <c>Context</c> property of each calendar item.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for whom the calendar items are retrieved.</param>
        /// <param name="familyId">The unique identifier of the family associated with the progeny.</param>
        /// <param name="context">A string used to filter the calendar items by their context. Only items whose context contains the specified
        /// string  (case-insensitive) will be included. If <see langword="null"/> or empty, no filtering is applied.</param>
        /// <param name="currentUserInfo">The user information of the currently authenticated user, used for authorization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="CalendarItem"/>  objects matching the specified criteria. If no items match, an empty list is
        /// returned.</returns>
        public async Task<List<CalendarItem>> GetCalendarItemsWithContext(int progenyId, int familyId, string context, UserInfo currentUserInfo)
        {
            List<CalendarItem> allItems = await GetCalendarList(progenyId, familyId, currentUserInfo);
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
            if (allItems.Count != 0)
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
