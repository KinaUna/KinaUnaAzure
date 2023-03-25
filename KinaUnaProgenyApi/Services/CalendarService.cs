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
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(1, 0, 0, 0)); // Expire after a week.
        }

        public async Task<CalendarItem> GetCalendarItem(int id)
        {
            CalendarItem calendarItem = await GetCalendarItemFromCache(id);
            if (calendarItem == null || calendarItem.EventId == 0)
            {
                calendarItem = await SetCalendarItemInCache(id);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> AddCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToAdd = new();
            calendarItemToAdd.CopyPropertiesForAdd(item);

            _ = _context.CalendarDb.Add(calendarItemToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetCalendarItemInCache(calendarItemToAdd.EventId);

            return calendarItemToAdd;
        }

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

        private async Task<CalendarItem> SetCalendarItemInCache(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            if (calendarItem != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);

                List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + calendarItem.ProgenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarItem;
        }

        private async Task RemoveCalendarItemFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToUpdate = await _context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == item.EventId);
            if (calendarItemToUpdate != null)
            {
                calendarItemToUpdate.CopyPropertiesForUpdate(item);

                _ = _context.CalendarDb.Update(calendarItemToUpdate);
                _ = await _context.SaveChangesAsync();
                _ = await SetCalendarItemInCache(calendarItemToUpdate.EventId);
            }

            return calendarItemToUpdate;
        }

        public async Task<CalendarItem> DeleteCalendarItem(CalendarItem item)
        {
            CalendarItem calendarItemToDelete = await _context.CalendarDb.SingleOrDefaultAsync(ci => ci.EventId == item.EventId);
            if (calendarItemToDelete != null)
            {
                _ = _context.CalendarDb.Remove(calendarItemToDelete);
                _ = await _context.SaveChangesAsync();

                await RemoveCalendarItemFromCache(item.EventId, item.ProgenyId);
            }

            return item;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList = await GetCalendarListFromCache(progenyId);
            if (calendarList == null || calendarList.Count == 0)
            {
                calendarList = await SetCalendarListInCache(progenyId);
            }

            return calendarList;
        }

        private async Task<List<CalendarItem>> GetCalendarListFromCache(int progenyId)
        {
            List<CalendarItem> calendarList = new();
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }

            return calendarList;
        }

        private async Task<List<CalendarItem>> SetCalendarListInCache(int progenyId)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);

            return calendarList;
        }

    }
}
