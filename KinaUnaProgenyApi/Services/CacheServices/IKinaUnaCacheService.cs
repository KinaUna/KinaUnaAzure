using System.Collections.Generic;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaProgenyApi.Services.CacheServices
{
    public interface IKinaUnaCacheService
    {
        /// <summary>
        /// Sets the user updated cache entry for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        void SetUserUpdatedCache(string userId);

        /// <summary>
        /// Retrieves the cached user update entry for the specified user identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="UserUpdatedCacheEntry"/> object containing the cached update information for the user, or <see
        /// langword="null"/> if no cache entry exists for the specified user.</returns>
        UserUpdatedCacheEntry GetUserUpdatedCache(string userId);

        /// <summary>
        /// Sets the user updated cache entries for all members of the specified group.
        /// </summary>
        /// <param name="userGroupMembers">The collection of group members whose user updated cache entries are to be set.</param>
        void SetUserUpdatedCacheForGroup(IEnumerable<UserGroupMember> userGroupMembers);

        /// <summary>
        /// Sets the progeny or family updated cache entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <param name="familyId">The unique identifier of the family.</param>
        void SetProgenyOrFamilyUpdatedCache(int progenyId, int familyId);

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        ProgenyUpdatedCacheEntry GetProgenyOrFamilyUpdatedCache(int progenyId, int familyId);
    }
}
