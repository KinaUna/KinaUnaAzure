using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Timeline;
using KinaUnaProgenyApi.Helpers;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly ITimelineFilteringService _timelineFilteringService;
        private readonly ICalendarService _calendarService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public TimelineService(ProgenyDbContext context, ITimelineFilteringService timelineFilteringService, IDistributedCache cache, ICalendarService calendarService, IAccessManagementService accessManagementService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _timelineFilteringService = timelineFilteringService;
            _calendarService = calendarService;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified TimeLineId.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The TimeLineItem with the given TimeLineId. Null if the TimeLineItem doesn't exist.</returns>
        public async Task<TimeLineItem> GetTimeLineItem(int id, UserInfo currentUserInfo)
        {
            if (id == 0)
            {
                return null;
            }

            TimeLineItem timeLineItem = await GetTimeLineItemFromCache(id);
            if (timeLineItem == null || timeLineItem.TimeLineId == 0)
            {
                timeLineItem = await SetTimeLineItemInCache(id);
            }

            _ = int.TryParse(timeLineItem.ItemId, out int itemId);
            KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
            if (itemId <= 0) return null;
            
            if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
            {
                return timeLineItem;
            }

            return null;
        }

        /// <summary>
        /// Adds a new TimeLineItem to the database and adds it to the cache.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added TimeLineItem.</returns>
        public async Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem, UserInfo currentUserInfo)
        {
            TimeLineItem existingTimeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == timeLineItem.ItemId && t.ItemType == timeLineItem.ItemType);
            // Only add the TimeLineItem if it doesn't already exist, and if it is linked to either a Progeny or a Family.
            if (existingTimeLineItem != null || (timeLineItem.ProgenyId > 0 && timeLineItem.FamilyId > 0))
            {
                return null;
            }

            if (timeLineItem.ProgenyId == 0 && timeLineItem.FamilyId == 0)
            {
                return null;
            }

            if (timeLineItem.ProgenyId > 0)
            {
                if (!await _accessManagementService.HasProgenyPermission(timeLineItem.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    return null;
                }
            }

            if (timeLineItem.FamilyId > 0)
            {
                if (!await _accessManagementService.HasFamilyPermission(timeLineItem.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    return null;
                }
            }

            TimeLineItem timeLineItemToAdd = new();
            timeLineItemToAdd.CopyPropertiesForAdd(timeLineItem);

            _ = _context.TimeLineDb.Add(timeLineItemToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetTimeLineItemInCache(timeLineItemToAdd.TimeLineId);

            return timeLineItemToAdd;
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified TimeLineId from the cache.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to get.</param>
        /// <returns>The TimeLineItem with the given TimeLineId. Null if it isn't found in the cache.</returns>
        private async Task<TimeLineItem> GetTimeLineItemFromCache(int id)
        {
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id);
            if (string.IsNullOrEmpty(cachedTimeLineItem))
            {
                return null;
            }

            TimeLineItem timeLineItem = JsonSerializer.Deserialize<TimeLineItem>(cachedTimeLineItem, JsonSerializerOptions.Web);
            return timeLineItem;
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified TimeLineId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to get and set.</param>
        /// <returns>The TimeLineItem with the given TimeLineId. Null if the TimeLineItem doesn't exist.</returns>
        private async Task<TimeLineItem> SetTimeLineItemInCache(int id)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(t => t.TimeLineId == id);
            if (timeLineItem == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id, JsonSerializer.Serialize(timeLineItem, JsonSerializerOptions.Web), _cacheOptionsSliding);
            _ = await SetTimeLineItemByItemIdInCache(timeLineItem.ItemId, timeLineItem.ItemType);
            _ = await SetTimeLineListInCache(timeLineItem.ProgenyId, timeLineItem.FamilyId);

            return timeLineItem;
        }

        /// <summary>
        /// Updates a TimeLineItem in the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't already exist.</returns>
        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem item, UserInfo currentUserInfo)
        {
            
            TimeLineItem timeLineItemToUpdate = await _context.TimeLineDb.SingleOrDefaultAsync(ti => ti.TimeLineId == item.TimeLineId);

            // Only update the TimeLineItem if it exists, and if it is linked to either a Progeny or a Family.
            if (timeLineItemToUpdate == null || (item.ProgenyId > 0 && item.FamilyId > 0))
            {
                return null;
            }

            // ProgenyId or FamilyId must be set.
            if (item.ProgenyId == 0 && item.FamilyId == 0)
            {
                return null;
            }

            bool assignedToDifferentEntity = false;
            if (timeLineItemToUpdate.ProgenyId != item.ProgenyId || timeLineItemToUpdate.FamilyId != item.FamilyId)
            {
                assignedToDifferentEntity = true;
                // For now, only TodoItems are allowed to change ProgenyId or FamilyId.
                // Todo: Consider other items, such as Calendar.
                if (timeLineItemToUpdate.ItemType != (int)KinaUnaTypes.TimeLineType.TodoItem)
                {
                    // ProgenyId or FamilyId change is not allowed.
                    return null;
                } 
            }

            KinaUnaTypes.TimeLineType itemTypeAsTimelineType = (KinaUnaTypes.TimeLineType)item.ItemType;
            _ = int.TryParse(item.ItemId, out int itemIdAsInt);
            if (!await _accessManagementService.HasItemPermission(itemTypeAsTimelineType, itemIdAsInt, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }
            
            timeLineItemToUpdate.CopyPropertiesForUpdate(item);
            if (assignedToDifferentEntity)
            {
                timeLineItemToUpdate.ProgenyId = item.ProgenyId;
                timeLineItemToUpdate.FamilyId = item.FamilyId;
            }

            _ = _context.TimeLineDb.Update(timeLineItemToUpdate);
            _ = await _context.SaveChangesAsync();
            // No need to update permissions, as they are linked to the ItemType and ItemId, not the TimeLineItem itself.

            _ = await SetTimeLineItemInCache(item.TimeLineId);

            return item;
        }

        /// <summary>
        /// Deletes a TimeLineItem from the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't exist.</returns>
        public async Task<TimeLineItem> DeleteTimeLineItem(TimeLineItem item, UserInfo currentUserInfo)
        {
            if (item == null || item.ItemId == "0") return null;

            TimeLineItem timeLineItemToDelete = await _context.TimeLineDb.SingleOrDefaultAsync(ti => ti.TimeLineId == item.TimeLineId);
            KinaUnaTypes.TimeLineType itemTypeAsTimelineType = (KinaUnaTypes.TimeLineType)item.ItemType;
            _ = int.TryParse(item.ItemId, out int itemIdAsInt);
            if (!await _accessManagementService.HasItemPermission(itemTypeAsTimelineType, itemIdAsInt, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            if (timeLineItemToDelete != null)
            {
                _ = _context.TimeLineDb.Remove(timeLineItemToDelete);
                _ = await _context.SaveChangesAsync();
            }

            await RemoveTimeLineItemFromCache(item.TimeLineId, item.ItemType, item.ProgenyId, item.FamilyId);

            return item;
        }

        /// <summary>
        /// Removes a TimeLineItem from the cache and updates the TimeLineList for the Progeny in the cache.
        /// </summary>
        /// <param name="timeLineItemId">The ItemId of the TimeLineItem to remove.</param>
        /// <param name="timeLineType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <param name="progenyId">The ProgenyId of the TimeLineItem.</param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        private async Task RemoveTimeLineItemFromCache(int timeLineItemId, int timeLineType, int progenyId, int familyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + timeLineItemId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + timeLineItemId + "type" + timeLineType);
            _ = await SetTimeLineListInCache(progenyId, familyId);
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified ItemId and ItemType.
        /// First checks the cache, if not found, gets the TimeLineItem from the database and adds it to the cache.
        /// </summary>
        /// <param name="itemId">The ItemId of the TimeLineItem.</param>
        /// <param name="itemType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The TimeLineItem with the given ItemId and ItemType. Null if the TimeLineItem doesn't exist.</returns>
        public async Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType, UserInfo currentUserInfo)
        {
            _ = int.TryParse(itemId, out int itemIdAsInt);
            KinaUnaTypes.TimeLineType itemTypeAsTimelineType = (KinaUnaTypes.TimeLineType)itemType;
            if (itemIdAsInt <= 0) return null;

            if (!await _accessManagementService.HasItemPermission(itemTypeAsTimelineType, itemIdAsInt, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            TimeLineItem timeLineItem = await GetTimeLineItemByItemIdFromCache(itemId, itemType);
            if (timeLineItem == null || timeLineItem.TimeLineId == 0)
            {
                timeLineItem = await SetTimeLineItemByItemIdInCache(itemId, itemType);
            }

            return timeLineItem;
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified ItemId and ItemType from the cache.
        /// </summary>
        /// <param name="itemId">The ItemId of the TimeLineItem.</param>
        /// <param name="itemType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <returns>The TimeLineItem with the given ItemId and ItemType. Null if the TimeLineItem isn't found in the cache.</returns>
        private async Task<TimeLineItem> GetTimeLineItemByItemIdFromCache(string itemId, int itemType)
        {
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + itemId + itemType);
            if (string.IsNullOrEmpty(cachedTimeLineItem))
            {
                return null;
            }

            TimeLineItem timeLineItem = JsonSerializer.Deserialize<TimeLineItem>(cachedTimeLineItem, JsonSerializerOptions.Web);
            return timeLineItem;
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified ItemId and ItemType from the database and adds it to the cache.
        /// </summary>
        /// <param name="itemId">The ItemId of the TimeLineItem.</param>
        /// <param name="itemType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <returns>The TimeLineItem with the given ItemId and ItemType. Null if the TimeLineItem doesn't exist.</returns>
        private async Task<TimeLineItem> SetTimeLineItemByItemIdInCache(string itemId, int itemType)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == itemId && t.ItemType == itemType);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + itemId + "type" + itemType, JsonSerializer.Serialize(timeLineItem, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return timeLineItem;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineList(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            
            List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, familyId);
            if (timeLineList.Count == 0)
            {
                timeLineList = await SetTimeLineListInCache(progenyId, familyId);
            }

            List<TimeLineItem> filteredTimeLineList = [];
            foreach (TimeLineItem timeLineItem in timeLineList)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    filteredTimeLineList.Add(timeLineItem);
                }
            }
            return filteredTimeLineList;
        }

        public async Task<List<TimeLineItem>> GetFilteredTimeLineList(TimelineListRequest request, UserInfo currentUserInfo)
        {
            List<TimeLineItem> allTimelineItems = [];
            foreach (int progenyId in request.Progenies)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, 0);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(progenyId, 0);
                }

                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(progenyId, 0, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }
                
                allTimelineItems.AddRange(timeLineList);
                
            }
            foreach (int familyId in request.Families)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(0, familyId);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(0, familyId);
                }

                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(0, familyId, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }
                allTimelineItems.AddRange(timeLineList);
            }
            

            if (request.SortOrder == 1)
            {
                allTimelineItems = [.. allTimelineItems.OrderByDescending(t => t.ProgenyTime)];
            }
            else
            {
                allTimelineItems = [.. allTimelineItems.OrderBy(t => t.ProgenyTime)];
            }

            if (request.Year != 0)
            {
                DateTime startDate = new(request.Year, request.Month, request.Day, 23, 59, 59);
                if (request.SortOrder == 1)
                {

                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime <= startDate)];
                }
                else
                {
                    startDate = new(request.Year, request.Month, request.Day, 0, 0, 0);
                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime >= startDate)];
                }
            }

            List<TimeLineItem> filteredTimeLineList = [];
            int skipped = 0;
            int added = 0;
            foreach (TimeLineItem timeLineItem in allTimelineItems)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    if (skipped < request.Skip)
                    {
                        skipped++;
                    }
                    else if (added < request.Count)
                    {
                        added++;
                        filteredTimeLineList.Add(timeLineItem);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return filteredTimeLineList;
        }

        public async Task<int> GetTimeLineListFirstItemYear(List<int> progenies, List<int> families, UserInfo currentUserInfo)
        {
            List<TimeLineItem> allTimelineItems = [];
            foreach (int progenyId in progenies)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, 0);

                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(progenyId, 0);
                }

                allTimelineItems.AddRange(timeLineList);
            }

            foreach (int familyId in families)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(0, familyId);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(0, familyId);
                }

                allTimelineItems.AddRange(timeLineList);
            }

            allTimelineItems = [.. allTimelineItems.OrderBy(t => t.ProgenyTime)];

            int firstItemYear = DateTime.UtcNow.Year;
            foreach (TimeLineItem timeLineItem in allTimelineItems)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    firstItemYear = timeLineItem.ProgenyTime.Year;
                    break;
                }
            }

            return firstItemYear;
        }

        public async Task<List<TimeLineItem>> GetYearAgoList(int progenyId, int familyId, UserInfo currentUserInfo)
        {

            List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, familyId);
            if (timeLineList.Count == 0)
            {
                timeLineList = await SetTimeLineListInCache(progenyId, familyId);
            }

            timeLineList =
            [
                .. timeLineList
                    .Where(t => t.ProgenyTime.Year < DateTime.UtcNow.Year
                                && t.ProgenyTime.Month == DateTime.UtcNow.Month
                                && t.ProgenyTime.Day == DateTime.UtcNow.Day)
            ];

            List<TimeLineItem> filteredTimeLineList = [];
            foreach (TimeLineItem timeLineItem in timeLineList)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    filteredTimeLineList.Add(timeLineItem);
                }
            }

            return filteredTimeLineList;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="familyId"></param>
        /// <returns>List of TimeLineItem objects.</returns>
        private async Task<List<TimeLineItem>> GetTimeLineListFromCache(int progenyId, int familyId)
        {
            List<TimeLineItem> timeLineList = [];
            string cachedTimeLineList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId + "_family_" + familyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonSerializer.Deserialize<List<TimeLineItem>>(cachedTimeLineList, JsonSerializerOptions.Web);
            }

            return timeLineList;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="familyId"></param>
        /// <returns>List of TimeLineItem objects.</returns>
        private async Task<List<TimeLineItem>> SetTimeLineListInCache(int progenyId, int familyId)
        {
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId && t.FamilyId == familyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId + "_family_" + familyId, JsonSerializer.Serialize(timeLineList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return timeLineList;
        }

        /// <summary>
        /// Creates a OnThisDayResponse for displaying TimeLineItems on the OnThisDay page.
        /// </summary>
        /// <param name="onThisDayRequest">The OnThisDayRequest object with the parameters.</param>
        /// <param name="currentUserInfo">The current users UserInfo.</param>
        /// <returns>OnThisDayResponse object.</returns>
        public async Task<OnThisDayResponse> GetOnThisDayData(OnThisDayRequest onThisDayRequest, UserInfo currentUserInfo)
        {
            OnThisDayResponse onThisDayResponse = new()
            {
                Request = onThisDayRequest
            };

            List<TimeLineItem> allTimelineItems = [];
            foreach (int progenyId in onThisDayRequest.Progenies)
            {
                List<TimeLineItem> progenyTimeLineItems = [];
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, 0);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(progenyId, 0);
                }

                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(progenyId, 0, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }

                bool anyFilter = false;
                if (!string.IsNullOrEmpty(onThisDayRequest.TagFilter))
                {
                    anyFilter = true;
                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithTags(progenyId, 0, timeLineList, onThisDayRequest.TagFilter, currentUserInfo));

                }

                if (!string.IsNullOrEmpty(onThisDayRequest.CategoryFilter))
                {
                    anyFilter = true;
                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithCategories(progenyId, 0, timeLineList, onThisDayRequest.CategoryFilter, currentUserInfo));
                }

                if (!string.IsNullOrEmpty(onThisDayRequest.ContextFilter))
                {
                    anyFilter = true;

                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithContexts(progenyId, 0, timeLineList, onThisDayRequest.ContextFilter, currentUserInfo));
                }

                if (anyFilter)
                {
                    progenyTimeLineItems = [.. progenyTimeLineItems.Distinct()];
                }
                else
                {
                    progenyTimeLineItems = timeLineList;
                }

                allTimelineItems.AddRange(progenyTimeLineItems);
            }
            foreach (int familyId in onThisDayRequest.Families)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(0, familyId);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(0, familyId);
                }

                List<TimeLineItem> familyTimeLineItems = [];
                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(0, familyId, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }

                bool anyFilter = false;
                if (!string.IsNullOrEmpty(onThisDayRequest.TagFilter))
                {
                    anyFilter = true;
                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithTags(0, familyId, timeLineList, onThisDayRequest.TagFilter, currentUserInfo));

                }

                if (!string.IsNullOrEmpty(onThisDayRequest.CategoryFilter))
                {
                    anyFilter = true;
                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithCategories(0, familyId, timeLineList, onThisDayRequest.CategoryFilter, currentUserInfo));
                }

                if (!string.IsNullOrEmpty(onThisDayRequest.ContextFilter))
                {
                    anyFilter = true;

                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithContexts(0, familyId, timeLineList, onThisDayRequest.ContextFilter, currentUserInfo));
                }

                if (anyFilter)
                {
                    familyTimeLineItems = [.. familyTimeLineItems.Distinct()];
                }
                else
                {
                    familyTimeLineItems = timeLineList;
                }

                allTimelineItems.AddRange(familyTimeLineItems);

            }

            allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime <= DateTime.UtcNow)];
            if (allTimelineItems.Count == 0)
            {
                onThisDayResponse.TimeLineItems = [];
                onThisDayResponse.RemainingItemsCount = 0;
                return onThisDayResponse;
            }

            allTimelineItems = OnThisDayItemsFilters.FilterOnThisDayItemsByTimeLineType(allTimelineItems, onThisDayRequest.TimeLineTypeFilter);
            
            foreach (TimeLineItem timeLineItem in onThisDayResponse.TimeLineItems)
            {
                timeLineItem.ProgenyTime = TimeZoneInfo.ConvertTimeFromUtc(timeLineItem.ProgenyTime, TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
            }

            allTimelineItems = OnThisDayItemsFilters.FilterOnThisDayItemsByPeriod(allTimelineItems, onThisDayRequest);

            if (onThisDayRequest.SortOrder == 1)
            {
                allTimelineItems = [.. allTimelineItems.OrderByDescending(t => t.ProgenyTime)];
            }
            else
            {
                allTimelineItems = [.. allTimelineItems.OrderBy(t => t.ProgenyTime)];
            }

            if (onThisDayRequest.Year != 0)
            {
                DateTime startDate = new(onThisDayRequest.Year, onThisDayRequest.Month, onThisDayRequest.Day, 23, 59, 59);
                if (onThisDayRequest.SortOrder == 1)
                {

                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime <= startDate)];
                }
                else
                {
                    startDate = new(onThisDayRequest.Year, onThisDayRequest.Month, onThisDayRequest.Day, 0, 0, 0);
                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime >= startDate)];
                }
            }

            List<TimeLineItem> filteredTimeLineList = [];
            int skipped = 0;
            int added = 0;
            foreach (TimeLineItem timeLineItem in allTimelineItems)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    if (skipped < onThisDayRequest.Skip)
                    {
                        skipped++;
                        continue;
                    }

                    if (added >= 2 * onThisDayRequest.NumberOfItems)
                    {
                        break;
                    }

                    added++;
                    filteredTimeLineList.Add(timeLineItem);
                }
            }

            onThisDayResponse.Request.FirstItemYear = await GetTimeLineListFirstItemYear(onThisDayRequest.Progenies, onThisDayRequest.Families, currentUserInfo);
            onThisDayResponse.RemainingItemsCount = filteredTimeLineList.Count - onThisDayRequest.NumberOfItems;
            onThisDayResponse.TimeLineItems = [.. filteredTimeLineList.Take(onThisDayRequest.NumberOfItems)];

            if (onThisDayResponse.RemainingItemsCount < 0)
            {
                onThisDayResponse.RemainingItemsCount = 0;
            }
            return onThisDayResponse;
        }

        /// <summary>
        /// Gets a TimelineResponse for displaying TimeLineItems on the Timeline page.
        /// </summary>
        /// <param name="timelineRequest">The TimelineRequest object with the parameters.</param>
        /// <param name="currentUserInfo">The current users UserInfo.</param>
        /// <returns>TimelineResponse with the filtered list of Timeline items.</returns>
        public async Task<TimelineResponse> GetTimelineData(TimelineRequest timelineRequest, UserInfo currentUserInfo)
        {
            TimelineResponse timelineResponse = new()
            {
                Request = timelineRequest
            };

            List<TimeLineItem> allTimelineItems = [];
            foreach (int progenyId in timelineRequest.Progenies)
            {
                List<TimeLineItem> progenyTimeLineItems = [];
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId, 0);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(progenyId, 0);
                }

                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(progenyId, 0, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }

                bool anyFilter = false;
                if (!string.IsNullOrEmpty(timelineRequest.TagFilter))
                {
                    anyFilter = true;
                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithTags(progenyId, 0, timeLineList, timelineRequest.TagFilter, currentUserInfo));
                    
                }

                if (!string.IsNullOrEmpty(timelineRequest.CategoryFilter))
                {
                    anyFilter = true;
                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithCategories(progenyId, 0, timeLineList, timelineRequest.CategoryFilter, currentUserInfo));
                }

                if (!string.IsNullOrEmpty(timelineRequest.ContextFilter))
                {
                    anyFilter = true;
                    
                    progenyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithContexts(progenyId, 0, timeLineList, timelineRequest.ContextFilter, currentUserInfo));
                }

                if (anyFilter)
                {
                    progenyTimeLineItems = [.. progenyTimeLineItems.Distinct()];
                }
                else
                {
                    progenyTimeLineItems = timeLineList;
                }
                
                allTimelineItems.AddRange(progenyTimeLineItems);
            }
            foreach (int familyId in timelineRequest.Families)
            {
                List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(0, familyId);
                if (timeLineList.Count == 0)
                {
                    timeLineList = await SetTimeLineListInCache(0, familyId);
                }

                List<TimeLineItem> familyTimeLineItems = [];
                List<CalendarItem> calendarItems = await _calendarService.GetRecurringCalendarItemsLatestPosts(0, familyId, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await _calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }

                bool anyFilter = false;
                if (!string.IsNullOrEmpty(timelineRequest.TagFilter))
                {
                    anyFilter = true;
                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithTags(0, familyId, timeLineList, timelineRequest.TagFilter, currentUserInfo));

                }

                if (!string.IsNullOrEmpty(timelineRequest.CategoryFilter))
                {
                    anyFilter = true;
                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithCategories(0, familyId, timeLineList, timelineRequest.CategoryFilter, currentUserInfo));
                }

                if (!string.IsNullOrEmpty(timelineRequest.ContextFilter))
                {
                    anyFilter = true;

                    familyTimeLineItems.AddRange(await _timelineFilteringService.GetTimeLineItemsWithContexts(0, familyId, timeLineList, timelineRequest.ContextFilter, currentUserInfo));
                }

                if (anyFilter)
                {
                    familyTimeLineItems = [.. familyTimeLineItems.Distinct()];
                }
                else
                {
                    familyTimeLineItems = timeLineList;
                }

                allTimelineItems.AddRange(familyTimeLineItems);

            }

            allTimelineItems = OnThisDayItemsFilters.FilterOnThisDayItemsByTimeLineType(allTimelineItems, timelineRequest.TimeLineTypeFilter);

            foreach (TimeLineItem timeLineItem in allTimelineItems)
            {
                timeLineItem.ProgenyTime = TimeZoneInfo.ConvertTimeFromUtc(timeLineItem.ProgenyTime, TimeZoneInfo.FindSystemTimeZoneById(currentUserInfo.Timezone));
            }

            if (timelineRequest.SortOrder == 1)
            {
                allTimelineItems = [.. allTimelineItems.OrderByDescending(t => t.ProgenyTime)];
            }
            else
            {
                allTimelineItems = [.. allTimelineItems.OrderBy(t => t.ProgenyTime)];
            }

            if (timelineRequest.Year != 0)
            {
                DateTime startDate = new(timelineRequest.Year, timelineRequest.Month, timelineRequest.Day, 23, 59, 59);
                if (timelineRequest.SortOrder == 1)
                {

                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime <= startDate)];
                }
                else
                {
                    startDate = new(timelineRequest.Year, timelineRequest.Month, timelineRequest.Day, 0, 0, 0);
                    allTimelineItems = [.. allTimelineItems.Where(t => t.ProgenyTime >= startDate)];
                }
            }

            if (allTimelineItems.Count == 0)
            {
                timelineResponse.TimeLineItems = [];
                timelineResponse.RemainingItemsCount = 0;
                return timelineResponse;
            }

            List<TimeLineItem> filteredTimeLineList = [];
            int skipped = 0;
            int added = 0;
            foreach (TimeLineItem timeLineItem in allTimelineItems)
            {
                _ = int.TryParse(timeLineItem.ItemId, out int itemId);
                KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timeLineItem.ItemType;
                if (itemId <= 0) continue;
                if (await _accessManagementService.HasItemPermission(itemType, itemId, currentUserInfo, PermissionLevel.View))
                {
                    if (skipped < timelineRequest.Skip)
                    {
                        skipped++;
                        continue;
                    }
                    if (added >= 2 * timelineRequest.NumberOfItems)
                    {
                        break;
                    }
                    added++;
                    filteredTimeLineList.Add(timeLineItem);
                }
            }

            
            timelineResponse.Request.FirstItemYear = await GetTimeLineListFirstItemYear(timelineRequest.Progenies, timelineRequest.Families, currentUserInfo);
            
            timelineResponse.RemainingItemsCount =  filteredTimeLineList.Count - timelineRequest.NumberOfItems;
            timelineResponse.TimeLineItems = [.. filteredTimeLineList.Take(timelineRequest.NumberOfItems)];

            if (timelineResponse.RemainingItemsCount < 0)
            {
                timelineResponse.RemainingItemsCount = 0;
            }

            return timelineResponse;
        }
    }
}
