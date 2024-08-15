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

namespace KinaUnaProgenyApi.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public TimelineService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified TimeLineId.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to get.</param>
        /// <returns>The TimeLineItem with the given TimeLineId. Null if the TimeLineItem doesn't exist.</returns>
        public async Task<TimeLineItem> GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem = await GetTimeLineItemFromCache(id);
            if (timeLineItem == null || timeLineItem.TimeLineId == 0)
            {
                timeLineItem = await SetTimeLineItemInCache(id);
            }

            return timeLineItem;
        }

        /// <summary>
        /// Adds a new TimeLineItem to the database and adds it to the cache.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem to add.</param>
        /// <returns>The added TimeLineItem.</returns>
        public async Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem)
        {
            TimeLineItem existingTimeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == timeLineItem.ItemId && t.ItemType == timeLineItem.ItemType);
            if (existingTimeLineItem != null)
            {
                return timeLineItem;
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

            TimeLineItem timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
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

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            _ = await SetTimeLineItemByItemIdInCache(timeLineItem.ItemId, timeLineItem.ItemType);
            _ = await SetTimeLineListInCache(timeLineItem.ProgenyId);

            return timeLineItem;
        }

        /// <summary>
        /// Updates a TimeLineItem in the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem with the updated properties.</param>
        /// <returns>The updated TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't already exist.</returns>
        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem item)
        {
            TimeLineItem timeLineItemToUpdate = await _context.TimeLineDb.SingleOrDefaultAsync(ti => ti.TimeLineId == item.TimeLineId);
            if (timeLineItemToUpdate == null) return null;

            timeLineItemToUpdate.CopyPropertiesForUpdate(item);

            _ = _context.TimeLineDb.Update(timeLineItemToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetTimeLineItemInCache(item.TimeLineId);

            return item;
        }

        /// <summary>
        /// Deletes a TimeLineItem from the database and the cache.
        /// </summary>
        /// <param name="item">The TimeLineItem to delete.</param>
        /// <returns>The deleted TimeLineItem. Null if a TimeLineItem with the TimeLineId doesn't exist.</returns>
        public async Task<TimeLineItem> DeleteTimeLineItem(TimeLineItem item)
        {
            TimeLineItem timeLineItemToDelete = await _context.TimeLineDb.SingleOrDefaultAsync(ti => ti.TimeLineId == item.TimeLineId);
            if (timeLineItemToDelete != null)
            {
                _ = _context.TimeLineDb.Remove(timeLineItemToDelete);
                _ = await _context.SaveChangesAsync();
            }

            await RemoveTimeLineItemFromCache(item.TimeLineId, item.ItemType, item.ProgenyId);

            return item;
        }

        /// <summary>
        /// Removes a TimeLineItem from the cache and updates the TimeLineList for the Progeny in the cache.
        /// </summary>
        /// <param name="timeLineItemId">The ItemId of the TimeLineItem to remove.</param>
        /// <param name="timeLineType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <param name="progenyId">The ProgenyId of the TimeLineItem.</param>
        /// <returns></returns>
        private async Task RemoveTimeLineItemFromCache(int timeLineItemId, int timeLineType, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + timeLineItemId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + timeLineItemId + "type" + timeLineType);
            _ = await SetTimeLineListInCache(progenyId);
        }

        /// <summary>
        /// Gets the TimeLineItem with the specified ItemId and ItemType.
        /// First checks the cache, if not found, gets the TimeLineItem from the database and adds it to the cache.
        /// </summary>
        /// <param name="itemId">The ItemId of the TimeLineItem.</param>
        /// <param name="itemType">The ItemType (see KinaUnaTypes.TimeLineTypes enum) of the TimeLineItem.</param>
        /// <returns>The TimeLineItem with the given ItemId and ItemType. Null if the TimeLineItem doesn't exist.</returns>
        public async Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType)
        {
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

            TimeLineItem timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + itemId + "type" + itemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);

            return timeLineItem;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineList(int progenyId)
        {
            List<TimeLineItem> timeLineList = await GetTimeLineListFromCache(progenyId);
            if (timeLineList.Count == 0)
            {
                timeLineList = await SetTimeLineListInCache(progenyId);
            }

            return timeLineList;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        private async Task<List<TimeLineItem>> GetTimeLineListFromCache(int progenyId)
        {
            List<TimeLineItem> timeLineList = [];
            string cachedTimeLineList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(cachedTimeLineList);
            }

            return timeLineList;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        private async Task<List<TimeLineItem>> SetTimeLineListInCache(int progenyId)
        {
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);

            return timeLineList;
        }

    }
}
