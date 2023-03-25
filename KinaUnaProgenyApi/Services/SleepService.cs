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
    public class SleepService : ISleepService
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
            Sleep sleep = await GetSleepFromCache(id);
            if (sleep == null || sleep.SleepId == 0)
            {
                sleep = await SetSleepInCache(id);
            }

            return sleep;
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            Sleep sleepToAdd = new Sleep();
            sleepToAdd.CopyPropertiesForAdd(sleep);

            _ = _context.SleepDb.Add(sleepToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetSleepInCache(sleepToAdd.SleepId);

            return sleepToAdd;
        }

        private async Task<Sleep> GetSleepFromCache(int id)
        {
            Sleep sleep = new Sleep();
            string cachedSleep = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }

            return sleep;
        }

        private async Task<Sleep> SetSleepInCache(int id)
        {
            Sleep sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleep != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

                _ = await SetSleepListInCache(sleep.ProgenyId);
            }

            return sleep;
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            Sleep sleepToUpdate = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToUpdate != null)
            {
                sleepToUpdate.CopyPropertiesForUpdate(sleep);

                _ = _context.SleepDb.Update(sleepToUpdate);
                _ = await _context.SaveChangesAsync();

                _ = await SetSleepInCache(sleepToUpdate.SleepId);
            }

            return sleepToUpdate;
        }

        public async Task<Sleep> DeleteSleep(Sleep sleep)
        {
            Sleep sleepToDelete = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToDelete != null)
            {
                _ = _context.SleepDb.Remove(sleepToDelete);
                _ = await _context.SaveChangesAsync();

                await RemoveSleep(sleepToDelete.SleepId, sleepToDelete.ProgenyId);
            }

            return sleep;
        }

        private async Task RemoveSleep(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);

            _ = await SetSleepListInCache(progenyId);
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList = await GetSleepListFromCache(progenyId);
            if (!sleepList.Any())
            {
                sleepList = await SetSleepListInCache(progenyId);
            }

            return sleepList;
        }

        private async Task<List<Sleep>> GetSleepListFromCache(int progenyId)
        {
            List<Sleep> sleepList = new List<Sleep>();
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }

            return sleepList;
        }

        private async Task<List<Sleep>> SetSleepListInCache(int progenyId)
        {
            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);

            return sleepList;
        }
    }
}
