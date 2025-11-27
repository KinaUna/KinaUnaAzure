using KinaUna.Data.Models.AccessManagement;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text.Json;
using KinaUna.Data;

namespace KinaUnaProgenyApi.Services.CacheServices
{
    public class KinaUnaInMemoryCacheService(IDistributedCache cache) : IKinaUnaCacheService
    {
        /// <summary>
        /// Sets the user updated cache entry for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        public void SetUserUpdatedCache(string userId)
        {
            UserUpdatedCacheEntry userCacheEntry = new()
            {
                UserId = userId,
                UpdateTime = DateTime.UtcNow
            };
            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userId
                , JsonSerializer.Serialize(userCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached user update entry for the specified user identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="UserUpdatedCacheEntry"/> object containing the cached update information for the user, or <see
        /// langword="null"/> if no cache entry exists for the specified user.</returns>
        public UserUpdatedCacheEntry GetUserUpdatedCache(string userId)
        {
            string cachedUserEntry = cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userId).Result;
            if (!string.IsNullOrEmpty(cachedUserEntry))
            {
                UserUpdatedCacheEntry userCacheEntry = JsonSerializer.Deserialize<UserUpdatedCacheEntry>(cachedUserEntry, JsonSerializerOptions.Web);
                return userCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Sets the user updated cache entries for all members of the specified group.
        /// </summary>
        /// <param name="groupMembers">The collection of group members whose user updated cache entries are to be set.</param>
        public void SetUserUpdatedCacheForGroup(IEnumerable<UserGroupMember> groupMembers)
        {
            foreach (UserGroupMember member in groupMembers)
            {
                if (!string.IsNullOrEmpty(member.UserId))
                {
                    SetUserUpdatedCache(member.UserId);
                }
            }
        }

        /// <summary>
        /// Sets the progeny or family updated cache entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <param name="familyId">The unique identifier of the family.</param>
        public void SetProgenyOrFamilyUpdatedCache(int progenyId, int familyId)
        {
            ProgenyUpdatedCacheEntry progenyCacheEntry = new()
            {
                ProgenyId = progenyId,
                UpdateTime = DateTime.UtcNow
            };
            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyOrFamilyCacheEntry_p_" + progenyId + "_f_" + familyId
                , JsonSerializer.Serialize(progenyCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        public ProgenyUpdatedCacheEntry GetProgenyOrFamilyUpdatedCache(int progenyId, int familyId)
        {
            string cachedUserEntry = cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyOrFamilyCacheEntry_p_" + progenyId + "_f_" + familyId).Result;
            if (!string.IsNullOrEmpty(cachedUserEntry))
            {
                ProgenyUpdatedCacheEntry progenyCacheEntry = JsonSerializer.Deserialize<ProgenyUpdatedCacheEntry>(cachedUserEntry, JsonSerializerOptions.Web);
                return progenyCacheEntry;
            }
            return null;
        }

        public void SetProgenyTimelineUpdatedCache(int progenyId)
        {
            ProgenyUpdatedCacheEntry progenyCacheEntry = new()
            {
                ProgenyId = progenyId,
                UpdateTime = DateTime.UtcNow
            };
            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyCacheEntry_" + progenyId
                , JsonSerializer.Serialize(progenyCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        public ProgenyUpdatedCacheEntry GetProgenyTimlineUpdatedCache(int progenyId)
        {
            string cachedUserEntry = cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyCacheEntry_" + progenyId).Result;
            if (!string.IsNullOrEmpty(cachedUserEntry))
            {
                ProgenyUpdatedCacheEntry progenyCacheEntry = JsonSerializer.Deserialize<ProgenyUpdatedCacheEntry>(cachedUserEntry, JsonSerializerOptions.Web);
                return progenyCacheEntry;
            }
            return null;
        }
    }
}
