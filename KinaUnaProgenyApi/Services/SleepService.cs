using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class SleepService : ISleepService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public SleepService(ProgenyDbContext context, IDistributedCache cache, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the Sleep with the specified SleepId.
        /// First checks the cache, if not found, gets the Sleep from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user. For checking permissions.</param>
        /// <returns>The Sleep object with the given SleepId. Null if the Sleep item doesn't exist.</returns>
        public async Task<Sleep> GetSleep(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Sleep sleep = await GetSleepFromCache(id);
            if (sleep == null || sleep.SleepId == 0)
            {
                sleep = await SetSleepInCache(id);
            }
            if (sleep == null || sleep.SleepId == 0)
            {
                return null;
            }
            sleep.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, sleep.SleepId, sleep.ProgenyId, 0, currentUserInfo);

            return sleep;
        }

        /// <summary>
        /// Adds a new Sleep to the database and adds it to the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Sleep object.</returns>
        public async Task<Sleep> AddSleep(Sleep sleep, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(sleep.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            Sleep sleepToAdd = new();
            sleepToAdd.CopyPropertiesForAdd(sleep);

            _ = _context.SleepDb.Add(sleepToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Sleep, sleepToAdd.SleepId, sleepToAdd.ProgenyId, 0, sleepToAdd.ItemPermissionsDtoList, currentUserInfo);

            _ = await SetSleepInCache(sleepToAdd.SleepId);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(sleepToAdd.ProgenyId, 0, KinaUnaTypes.TimeLineType.Sleep);

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
            
            Sleep sleep = JsonSerializer.Deserialize<Sleep>(cachedSleep, JsonSerializerOptions.Web);
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

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonSerializer.Serialize(sleep, JsonSerializerOptions.Web), _cacheOptionsSliding);

            _ = await SetSleepListInCache(sleep.ProgenyId);

            return sleep;
        }

        /// <summary>
        /// Updates a Sleep in the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Sleep object.</returns>
        public async Task<Sleep> UpdateSleep(Sleep sleep, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleep.SleepId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Sleep sleepToUpdate = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToUpdate == null) return null;

            sleepToUpdate.CopyPropertiesForUpdate(sleep);

            _ = _context.SleepDb.Update(sleepToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Sleep, sleepToUpdate.SleepId, sleepToUpdate.ProgenyId, 0, sleepToUpdate.ItemPermissionsDtoList, currentUserInfo);

            _ = await SetSleepInCache(sleepToUpdate.SleepId);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(sleepToUpdate.ProgenyId, 0, KinaUnaTypes.TimeLineType.Sleep);

            return sleepToUpdate;
        }

        /// <summary>
        /// Deletes a Sleep from the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Sleep object.</returns>
        public async Task<Sleep> DeleteSleep(Sleep sleep, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleep.SleepId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Sleep sleepToDelete = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == sleep.SleepId);
            if (sleepToDelete == null) return null;

            _ = _context.SleepDb.Remove(sleepToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveSleep(sleepToDelete.SleepId, sleepToDelete.ProgenyId);

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, sleepToDelete.SleepId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(sleepToDelete.ProgenyId, 0, KinaUnaTypes.TimeLineType.Sleep);

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
        /// <param name="currentUserInfo">The UserInfo object for the current user. For checking permissions.</param>
        /// <returns>List of Sleep objects.</returns>
        public async Task<List<Sleep>> GetSleepList(int progenyId, UserInfo currentUserInfo)
        {
            SleepListCacheEntry cacheEntry = _kinaUnaCacheService.GetSleepListCache(currentUserInfo.UserId, progenyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Sleep);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.SleepList.ToList();
                }
            }
            Sleep[] sleepList = await GetSleepListFromCache(progenyId);
            if (sleepList.Length == 0)
            {
                sleepList = await SetSleepListInCache(progenyId);
            }

            List<Sleep> filteredList = [];
            foreach (Sleep sleep in sleepList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, sleep.SleepId, currentUserInfo, PermissionLevel.View))
                {
                    sleep.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, sleep.SleepId, sleep.ProgenyId, 0, currentUserInfo);
                    filteredList.Add(sleep);
                }
            }

            _kinaUnaCacheService.SetSleepListCache(currentUserInfo.UserId, progenyId, filteredList.ToArray());

            return filteredList;
        }

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep items for.</param>
        /// <returns>List of Sleep objects.</returns>
        private async Task<Sleep[]> GetSleepListFromCache(int progenyId)
        {
            Sleep[] sleepList = [];
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonSerializer.Deserialize<Sleep[]>(cachedSleepList, JsonSerializerOptions.Web);
            }

            return sleepList;
        }

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny from the database and sets it in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set Sleep items for.</param>
        /// <returns>List of Sleep objects.</returns>
        private async Task<Sleep[]> SetSleepListInCache(int progenyId)
        {
            Sleep[] sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonSerializer.Serialize(sleepList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return sleepList;
        }
    }
}
