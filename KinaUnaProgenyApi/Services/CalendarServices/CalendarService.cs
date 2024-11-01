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
        /// <returns>List of CalendarItems.</returns>
        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel)
        {
            List<CalendarItem> calendarList = await GetCalendarListFromCache(progenyId);
            if (calendarList == null || calendarList.Count == 0)
            {
                calendarList = await SetCalendarListInCache(progenyId);
            }

            calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();

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
