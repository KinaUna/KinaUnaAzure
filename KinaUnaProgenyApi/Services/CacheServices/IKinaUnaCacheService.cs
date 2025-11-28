using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using System.Collections.Generic;
using static KinaUna.Data.Models.KinaUnaTypes;

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
        /// <returns>A <see cref="ProgenyOrFamilyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        ProgenyOrFamilyUpdatedCacheEntry GetProgenyOrFamilyUpdatedCache(int progenyId, int familyId);

        /// <summary>
        /// Sets the timeline updated cache entry for the specified progeny or family.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose timeline update cache entry is to be set. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose timeline update cache entry is to be set. Cannot be null or empty.</param>
        /// <param name="timelineType">The type of timeline update to set. Cannot be null or empty.</param>
        void SetProgenyOrFamilyTimelineUpdatedCache(int progenyId, int familyId, TimeLineType timelineType);

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="timelineType">The type of timeline update to retrieve. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyOrFamilyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        TimelineUpdatedCacheEntry GetProgenyOrFamilyTimelineUpdatedCache(int progenyId, int familyId, TimeLineType timelineType);

        /// <summary>
        /// Stores the specified item update information in the distributed cache with a sliding expiration of seven
        /// days.
        /// </summary>
        /// <remarks>The cached entry will expire if not accessed within seven days. The cache key is
        /// constructed using the application name, API version, item type, and item ID to ensure uniqueness.</remarks>
        /// <param name="itemType">The type of timeline to which the item belongs. Determines the cache key used for storage.</param>
        /// <param name="itemId">The unique identifier of the item whose update information is to be cached.</param>
        void SetItemUpdatedCache(TimeLineType itemType, int itemId);

        /// <summary>
        /// Retrieves the cached update entry for a specific item and timeline type, if available.
        /// </summary>
        /// <param name="itemType">The type of timeline to which the item belongs. Determines the cache key used for retrieval.</param>
        /// <param name="itemId">The unique identifier of the item whose update cache entry is to be retrieved.</param>
        /// <returns>An ItemUpdatedCacheEntry object containing the cached update information for the specified item and timeline
        /// type, or null if no cache entry exists.</returns>
        ItemUpdatedCacheEntry GetItemUpdatedCache(TimeLineType itemType, int itemId);

        /// <summary>
        /// Stores the specified list of notes in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached notes list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the notes list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the notes list.</param>
        /// <param name="notesList">The list of notes to cache. Cannot be null.</param>
        void SetNotesListCache(string userId, int progenyId, List<Note> notesList);

        /// <summary>
        /// Retrieves the cached notes list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose notes list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the notes list cache entry is requested.</param>
        /// <returns>A <see cref="NotesListCacheEntry"/> object containing the cached notes list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        NotesListCacheEntry GetNotesListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of pictures in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached pictures list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the pictures list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the pictures list.</param>
        /// <param name="picturesList">The list of pictures to cache. Cannot be null.</param>
        void SetPicturesListCache(string userId, int progenyId, List<Picture> picturesList);

        /// <summary>
        /// Retrieves the cached pictures list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose pictures list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the pictures list cache entry is requested.</param>
        /// <returns>A <see cref="PicturesListCacheEntry"/> object containing the cached pictures list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        PicturesListCacheEntry GetPicturesListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of contacts in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached contacts list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the contacts list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the contacts list.</param>
        /// <param name="familyId">The identifier of the family associated with the contacts list.</param>
        /// <param name="contactsList">The list of contacts to cache. Cannot be null.</param>
        void SetContactsListCache(string userId, int progenyId, int familyId, List<Contact> contactsList);

        /// <summary>
        /// Retrieves the cached contacts list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose contacts list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the contacts list cache entry is requested.</param>
        /// <param name="familyId">The identifier of the family for which the contacts list cache entry is requested.</param>
        /// <returns>A <see cref="ContactsListCacheEntry"/> object containing the cached contacts list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        ContactsListCacheEntry GetContactsListCache(string userId, int progenyId, int familyId);

        /// <summary>
        /// Stores the specified list of friends in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached friends list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the friends list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the friends list.</param>
        /// <param name="friendsList">The list of friends to cache. Cannot be null.</param>
        void SetFriendsListCache(string userId, int progenyId, List<Friend> friendsList);

        /// <summary>
        /// Retrieves the cached friends list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose friends list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the friends list cache entry is requested.</param>
        /// <returns>A <see cref="FriendsListCacheEntry"/> object containing the cached friends list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        FriendsListCacheEntry GetFriendsListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of locations in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached locations list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the locations list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the locations list.</param>
        /// <param name="familyId">The identifier of the family associated with the locations list.</param>
        /// <param name="locationsList">The list of locations to cache. Cannot be null.</param>
        void SetLocationsListCache(string userId, int progenyId, int familyId, List<Location> locationsList);

        /// <summary>
        /// Retrieves the cached locations list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose locations list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the locations list cache entry is requested.</param>
        /// <param name="familyId">The identifier of the family for which the locations list cache entry is requested.</param>
        /// <returns>A <see cref="LocationsListCacheEntry"/> object containing the cached locations list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        LocationsListCacheEntry GetLocationsListCache(string userId, int progenyId, int familyId);

        /// <summary>
        /// Stores the specified list of measurements in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached measurements list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the measurements list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the measurements list.</param>
        /// <param name="measurementsList">The list of measurements to cache. Cannot be null.</param>
        void SetMeasurementsListCache(string userId, int progenyId, List<Measurement> measurementsList);

        /// <summary>
        /// Retrieves the cached measurements list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose measurements list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the measurements list cache entry is requested.</param>
        /// <returns>A <see cref="MeasurementsListCacheEntry"/> object containing the cached measurements list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        MeasurementsListCacheEntry GetMeasurementsListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of skills in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached skills list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the skills list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the skills list.</param>
        /// <param name="skillsList">The list of skills to cache. Cannot be null.</param>
        void SetSkillsListCache(string userId, int progenyId, List<Skill> skillsList);

        /// <summary>
        /// Retrieves the cached skills list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose skills list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the skills list cache entry is requested.</param>
        /// <returns>A <see cref="SkillsListCacheEntry"/> object containing the cached skills list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        SkillsListCacheEntry GetSkillsListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of sleep in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached sleep list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the sleep list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the sleep list.</param>
        /// <param name="sleepList">The list of sleep to cache. Cannot be null.</param>
        void SetSleepListCache(string userId, int progenyId, List<Sleep> sleepList);

        /// <summary>
        /// Retrieves the cached sleep list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose sleep list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the sleep list cache entry is requested.</param>
        /// <returns>A <see cref="SleepListCacheEntry"/> object containing the cached sleep list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        SleepListCacheEntry GetSleepListCache(string userId, int progenyId);

        /// <summary>
        /// Stores the specified list of vaccinations in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached vaccinations list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the vaccinations list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the vaccinations list.</param>
        /// <param name="vaccinationsList">The list of vaccinations to cache. Cannot be null.</param>
        void SetVaccinationsListCache(string userId, int progenyId, List<Vaccination> vaccinationsList);

        /// <summary>
        /// Retrieves the cached vaccinations list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose vaccinations list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the vaccinations list cache entry is requested.</param>
        /// <returns>A <see cref="VaccinationsListCacheEntry"/> object containing the cached vaccinations list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        VaccinationsListCacheEntry GetVaccinationsListCache(string userId, int progenyId);

    }
}
