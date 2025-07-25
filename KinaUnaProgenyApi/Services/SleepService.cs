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
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public SleepService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the Sleep with the specified SleepId.
        /// First checks the cache, if not found, gets the Sleep from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep to get.</param>
        /// <returns>The Sleep object with the given SleepId. Null if the Sleep item doesn't exist.</returns>
        public async Task<Sleep> GetSleep(int id)
        {
            Sleep sleep = await GetSleepFromCache(id);
            if (sleep == null || sleep.SleepId == 0)
            {
                sleep = await SetSleepInCache(id);
            }

            return sleep;
        }

        /// <summary>
        /// Adds a new Sleep to the database and adds it to the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to add.</param>
        /// <returns>The added Sleep object.</returns>
        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            Sleep sleepToAdd = new();
            sleepToAdd.CopyPropertiesForAdd(sleep);

            _ = _context.SleepDb.Add(sleepToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetSleepInCache(sleepToAdd.SleepId);

            return sleepToAdd;
        }

        /// <summary>
        /// Gets the Sleep with the specified SleepId from the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to get.</param>
        /// <returns>The Sleep object with the given SleepId. Null if the Sleep item isn't found.</returns>
        private async Task<Sleep> GetSleepFromCache(int id)
        {
            string cachedSleep = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);
            if (string.IsNullOrEmpty(cachedSleep))
            {
                return null;
                
            }
            
            Sleep sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            return sleep;
        }

        /// <summary>
        /// Gets the Sleep with the specified SleepId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to get and set.</param>
        /// <returns>The Sleep object with the given SleepId. Null if the Sleep item doesn't exist.</returns>
        private async Task<Sleep> SetSleepInCache(int id)
        {
            Sleep sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleep == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

            _ = await SetSleepListInCache(sleep.ProgenyId);

            return sleep;
        }

        /// <summary>
        /// Updates a Sleep in the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object with the updated properties.</param>
        /// <returns>The updated Sleep object.</returns>
        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            Sleep sleepToUpdate = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToUpdate == null) return null;

            sleepToUpdate.CopyPropertiesForUpdate(sleep);

            _ = _context.SleepDb.Update(sleepToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetSleepInCache(sleepToUpdate.SleepId);

            return sleepToUpdate;
        }

        /// <summary>
        /// Deletes a Sleep from the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to delete.</param>
        /// <returns>The deleted Sleep object.</returns>
        public async Task<Sleep> DeleteSleep(Sleep sleep)
        {
            Sleep sleepToDelete = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToDelete == null) return null;

            _ = _context.SleepDb.Remove(sleepToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveSleep(sleepToDelete.SleepId, sleepToDelete.ProgenyId);

            return sleep;
        }

        /// <summary>
        /// Removes a Sleep from the cache and updates the SleepList for the Progeny in the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Sleep item belongs to.</param>
        /// <returns></returns>
        private async Task RemoveSleep(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);

            _ = await SetSleepListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep items for.</param>
        /// <param name="accessLevel">The access level for the current user.</param>
        /// <returns>List of Sleep objects.</returns>
        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> sleepList = await GetSleepListFromCache(progenyId);
            if (sleepList.Count == 0)
            {
                sleepList = await SetSleepListInCache(progenyId);
            }

            sleepList = [.. sleepList.Where(s => s.AccessLevel >= accessLevel)];

            return sleepList;
        }

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep items for.</param>
        /// <returns>List of Sleep objects.</returns>
        private async Task<List<Sleep>> GetSleepListFromCache(int progenyId)
        {
            List<Sleep> sleepList = [];
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }

            return sleepList;
        }

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set Sleep items for.</param>
        /// <returns>List of Sleep objects.</returns>
        private async Task<List<Sleep>> SetSleepListInCache(int progenyId)
        {
            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);

            return sleepList;
        }
    }
}
