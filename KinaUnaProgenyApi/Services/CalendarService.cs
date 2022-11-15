﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class CalendarService: ICalendarService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public CalendarService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<CalendarItem> GetCalendarItem(int id)
        {
            CalendarItem calendarItem;
            string cachedCalendarItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }
            else
            {
                calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> AddCalendarItem(CalendarItem item)
        {
            _context.CalendarDb.Add(item);
            await _context.SaveChangesAsync();

            await SetCalendarItem(item.EventId);

            return item;
        }

        public async Task<CalendarItem> SetCalendarItem(int id)
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

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem item)
        {
            _context.CalendarDb.Update(item);
            await _context.SaveChangesAsync();
            await SetCalendarItem(item.EventId);
            return item;
        }

        public async Task<CalendarItem> DeleteCalendarItem(CalendarItem item)
        {
            await RemoveCalendarItem(item.EventId, item.ProgenyId);
            _context.CalendarDb.Remove(item);
            await _context.SaveChangesAsync();
            
            return item;
        }
        public async Task RemoveCalendarItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarList;
        }

    }
}