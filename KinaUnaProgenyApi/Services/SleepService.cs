using System.Collections.Generic;
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
    public class SleepService: ISleepService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public SleepService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Sleep> GetSleep(int id)
        {
            Sleep sleep;
            string cachedSleep = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }
            else
            {
                sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);
            }

            return sleep;
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            _ = _context.SleepDb.Add(sleep);
            _ = await _context.SaveChangesAsync();
            _ = await SetSleep(sleep.SleepId);
            return sleep;
        }
        public async Task<Sleep> SetSleep(int id)
        {
            Sleep sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleep != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

                List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == sleep.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + sleep.ProgenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
            }
            
            return sleep;
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            _ = _context.SleepDb.Update(sleep);
            _ = await _context.SaveChangesAsync();
            _ = await SetSleep(sleep.SleepId);
            return sleep;
        }

        public async Task<Sleep> DeleteSleep(Sleep sleep)
        {
            _ = _context.SleepDb.Remove(sleep);
            _ = await _context.SaveChangesAsync();
            await RemoveSleep(sleep.SleepId, sleep.ProgenyId);

            return sleep;
        }

        public async Task RemoveSleep(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);

            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList;
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }
            else
            {
                sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
            }

            return sleepList;
        }
    }
}
