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
    public class VocabularyService : IVocabularyService
    {
        private readonly ProgenyDbContext _context;
        private readonly IUserInfoService _userInfoService;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public VocabularyService(ProgenyDbContext context, IDistributedCache cache, IUserInfoService userInfoService, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _context = context;
            _userInfoService = userInfoService;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a VocabularyItem entity with the specified WordId.
        /// First checks the cache, if not found, gets the VocabularyItem from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem.</param>
        /// <param name="currentUserInfo">The UserInfo object of the current user.For checking permissions.</param>
        /// <returns>The VocabularyItem with the given WordId. Null if the VocabularyItem doesn't exist.</returns>
        public async Task<VocabularyItem> GetVocabularyItem(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            VocabularyItem vocabularyItem = await GetVocabularyItemFromCache(id);
            if (vocabularyItem == null || vocabularyItem.WordId == 0)
            {
                vocabularyItem = await SetVocabularyItemInCache(id);
            }

            if(vocabularyItem != null && vocabularyItem.WordId != 0)
            {
                vocabularyItem.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, vocabularyItem.ProgenyId, 0, currentUserInfo);
            }

            return vocabularyItem;
        }

        /// <summary>
        /// Adds a new VocabularyItem entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem to add.</param>
        /// <returns>The added VocabularyItem.</returns>
        public async Task<VocabularyItem> AddVocabularyItem(VocabularyItem vocabularyItem)
        {
            UserInfo currentUserInfo = await _userInfoService.GetUserInfoByUserId(vocabularyItem.CreatedBy);
            if (!await _accessManagementService.HasProgenyPermission(vocabularyItem.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            VocabularyItem vocabularyItemToAdd = new();
            vocabularyItemToAdd.CopyPropertiesForAdd(vocabularyItem);

            _ = _context.VocabularyDb.Add(vocabularyItemToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItemToAdd.WordId, vocabularyItemToAdd.ProgenyId, 0, vocabularyItemToAdd.ItemPermissionsDtoList,
                currentUserInfo);

            _ = await SetVocabularyItemInCache(vocabularyItemToAdd.WordId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(vocabularyItemToAdd.ProgenyId, 0, KinaUnaTypes.TimeLineType.Vocabulary);
            return vocabularyItemToAdd;
        }

        /// <summary>
        /// Gets a VocabularyItem entity with the specified WordId from the cache.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to get.</param>
        /// <returns>The VocabularyItem with the given WordId. Null if the VocabularyItem isn't found in the cache.</returns>
        private async Task<VocabularyItem> GetVocabularyItemFromCache(int id)
        {
            string cachedVocabularyItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);
            if (string.IsNullOrEmpty(cachedVocabularyItem))
            {
                return null;
            }

            VocabularyItem vocabularyItem = JsonSerializer.Deserialize<VocabularyItem>(cachedVocabularyItem, JsonSerializerOptions.Web);
            return vocabularyItem;
        }

        /// <summary>
        /// Gets a VocabularyItem entity with the specified WordId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to get and set.</param>
        /// <returns>The VocabularyItem with the given WordId. Null if the item doesn't exist.</returns>
        private async Task<VocabularyItem> SetVocabularyItemInCache(int id)
        {
            VocabularyItem vocabularyItem = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            if (vocabularyItem == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonSerializer.Serialize(vocabularyItem, JsonSerializerOptions.Web), _cacheOptions);

            _ = await SetVocabularyListInCache(vocabularyItem.ProgenyId);

            return vocabularyItem;
        }

        /// <summary>
        /// Updates a VocabularyItem entity in the database and the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem with the updated properties.</param>
        /// <returns>The updated VocabularyItem object.</returns>
        public async Task<VocabularyItem> UpdateVocabularyItem(VocabularyItem vocabularyItem)
        {
            UserInfo currentUserInfo = await _userInfoService.GetUserInfoByUserId(vocabularyItem.ModifiedBy);
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            VocabularyItem vocabularyItemToUpdate = await _context.VocabularyDb.SingleOrDefaultAsync(v => v.WordId == vocabularyItem.WordId);
            if (vocabularyItemToUpdate == null) return null;

            vocabularyItemToUpdate.CopyPropertiesForUpdate(vocabularyItem);

            _ = _context.VocabularyDb.Update(vocabularyItemToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItemToUpdate.WordId, vocabularyItemToUpdate.ProgenyId, 0, vocabularyItemToUpdate.ItemPermissionsDtoList,
                currentUserInfo);

            _ = await SetVocabularyItemInCache(vocabularyItemToUpdate.WordId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(vocabularyItemToUpdate.ProgenyId, 0, KinaUnaTypes.TimeLineType.Vocabulary);

            return vocabularyItemToUpdate;
        }

        /// <summary>
        /// Deletes a VocabularyItem entity from the database and the cache.
        /// </summary>
        /// <param name="vocabularyItem">The VocabularyItem to delete.</param>
        /// <returns>The deleted VocabularyItem object.</returns>
        public async Task<VocabularyItem> DeleteVocabularyItem(VocabularyItem vocabularyItem)
        {
            UserInfo currentUserInfo = await _userInfoService.GetUserInfoByUserId(vocabularyItem.ModifiedBy);
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            VocabularyItem vocabularyItemToDelete = await _context.VocabularyDb.SingleOrDefaultAsync(v => v.WordId == vocabularyItem.WordId);
            if (vocabularyItemToDelete == null) return null;

            _ = _context.VocabularyDb.Remove(vocabularyItemToDelete);
            _ = await _context.SaveChangesAsync();

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, vocabularyItemToDelete.WordId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await RemoveVocabularyItemFromCache(vocabularyItem.WordId, vocabularyItem.ProgenyId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(vocabularyItemToDelete.ProgenyId, 0, KinaUnaTypes.TimeLineType.Vocabulary);

            return vocabularyItem;
        }

        /// <summary>
        /// Removes a VocabularyItem entity from the cache.
        /// Also updates the list of all VocabularyItems for the Progeny in the cache.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny the VocabularyItem belongs to.</param>
        /// <returns></returns>
        private async Task RemoveVocabularyItemFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);

            _ = await SetVocabularyListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all VocabularyItems for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        public async Task<List<VocabularyItem>> GetVocabularyList(int progenyId, UserInfo currentUserInfo)
        {
            VocabularyListCacheEntry cacheEntry = await _kinaUnaCacheService.GetVocabularyItemsListCache(currentUserInfo.UserId, progenyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = await _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Vocabulary);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.VocabularyList.ToList();
                }
            }

            VocabularyItem[] vocabularyList = await GetVocabularyListFromCache(progenyId);
            if (vocabularyList.Length == 0)
            {
                vocabularyList = await SetVocabularyListInCache(progenyId);
            }

            List<VocabularyItem> allowedVocabularyList = [];
            foreach (VocabularyItem vocabularyItem in vocabularyList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, currentUserInfo, PermissionLevel.View))
                {
                    //vocabularyItem.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, vocabularyItem.WordId, vocabularyItem.ProgenyId, 0, currentUserInfo);
                    allowedVocabularyList.Add(vocabularyItem);
                }
            }

            await _kinaUnaCacheService.SetVocabularyItemsListCache(currentUserInfo.UserId, progenyId, allowedVocabularyList.ToArray());

            return allowedVocabularyList;
        }

        /// <summary>
        /// Gets a list of all VocabularyItems for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list for.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        private async Task<VocabularyItem[]> GetVocabularyListFromCache(int progenyId)
        {
            VocabularyItem[] vocabularyList = [];
            string cachedVocabularyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonSerializer.Deserialize<VocabularyItem[]>(cachedVocabularyList, JsonSerializerOptions.Web);
            }

            return vocabularyList;
        }

        /// <summary>
        /// Gets a list of all VocabularyItems for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set the list for.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        private async Task<VocabularyItem[]> SetVocabularyListInCache(int progenyId)
        {
            VocabularyItem[] vocabularyList = await _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonSerializer.Serialize(vocabularyList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return vocabularyList;
        }
    }
}
