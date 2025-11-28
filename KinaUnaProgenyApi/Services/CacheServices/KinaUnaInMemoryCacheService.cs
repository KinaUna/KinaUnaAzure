using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static KinaUna.Data.Models.KinaUnaTypes;

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
            cache.SetString(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userId
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
            string cachedUserEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userId);
            if (!string.IsNullOrEmpty(cachedUserEntry))
            {
                UserUpdatedCacheEntry userCacheEntry = JsonSerializer.Deserialize<UserUpdatedCacheEntry>(cachedUserEntry, JsonSerializerOptions.Web);
                return userCacheEntry;
            }

            UserUpdatedCacheEntry emptyEntry = new()
            {
                UserId = userId,
                UpdateTime = DateTime.MinValue
            };
            return emptyEntry;
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
            ProgenyOrFamilyUpdatedCacheEntry progenyOrFamilyCacheEntry = new()
            {
                ProgenyId = progenyId,
                FamilyId = familyId,
                UpdateTime = DateTime.UtcNow
            };

            if (progenyId > 0)
            {
                
                DistributedCacheEntryOptions cacheOptionsSlidingView = new();
                cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
                cache.SetString(Constants.AppName + Constants.ApiVersion + "progenyCacheEntry_" + progenyId
                    , JsonSerializer.Serialize(progenyOrFamilyCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
            }

            if (familyId > 0)
            {
                DistributedCacheEntryOptions cacheOptionsSlidingView = new();
                cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
                cache.SetString(Constants.AppName + Constants.ApiVersion + "familyCacheEntry_" + familyId
                    , JsonSerializer.Serialize(progenyOrFamilyCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
            }
        }

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyOrFamilyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        public ProgenyOrFamilyUpdatedCacheEntry GetProgenyOrFamilyUpdatedCache(int progenyId, int familyId)
        {
            if (progenyId > 0)
            {
                string cachedProgenyEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "progenyCacheEntry_" + progenyId);
                if (!string.IsNullOrEmpty(cachedProgenyEntry))
                {
                    ProgenyOrFamilyUpdatedCacheEntry progenyCacheEntry = JsonSerializer.Deserialize<ProgenyOrFamilyUpdatedCacheEntry>(cachedProgenyEntry, JsonSerializerOptions.Web);
                    return progenyCacheEntry;
                }
            }

            if (familyId > 0)
            {
                string cachedFamilyEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "familyCacheEntry_" + familyId);
                if (!string.IsNullOrEmpty(cachedFamilyEntry))
                {
                    ProgenyOrFamilyUpdatedCacheEntry familyCacheEntry = JsonSerializer.Deserialize<ProgenyOrFamilyUpdatedCacheEntry>(cachedFamilyEntry, JsonSerializerOptions.Web);
                    return familyCacheEntry;
                }
            }

            ProgenyOrFamilyUpdatedCacheEntry emptyEntry = new()
            {
                ProgenyId = progenyId,
                FamilyId = familyId,
                UpdateTime = DateTime.MinValue
            };
            return emptyEntry;
        }

        /// <summary>
        /// Sets the timeline updated cache entry for the specified progeny or family.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose timeline update cache entry is to be set. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose timeline update cache entry is to be set. Cannot be null or empty.</param>
        /// <param name="timelineType">The type of timeline update to set. Cannot be null or empty.</param>
        public void SetProgenyOrFamilyTimelineUpdatedCache(int progenyId, int familyId, TimeLineType timelineType)
        {
            TimelineUpdatedCacheEntry timelineCacheEntry = new()
            {
                ProgenyId = progenyId,
                FamilyId = familyId,
                TimeLineType = timelineType,
                UpdateTime = DateTime.UtcNow
            };

            if (progenyId > 0)
            {
                
                DistributedCacheEntryOptions cacheOptionsSlidingView = new();
                cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
                cache.SetString(Constants.AppName + Constants.ApiVersion + "timelineCacheEntry_p_" + progenyId + "_t_" + (int)timelineType
                    , JsonSerializer.Serialize(timelineCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
            }

            if (familyId > 0)
            {
                DistributedCacheEntryOptions cacheOptionsSlidingView = new();
                cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
                cache.SetString(Constants.AppName + Constants.ApiVersion + "timelineCacheEntry_f_" + familyId + "_t_" + (int)timelineType
                    , JsonSerializer.Serialize(timelineCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
            }
        }

        /// <summary>
        /// Retrieves the cached user update entry for the specified progeny identifier.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="familyId">The unique identifier of the family whose updated cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="timelineType">The type of timeline update to retrieve. Cannot be null or empty.</param>
        /// <returns>A <see cref="ProgenyOrFamilyUpdatedCacheEntry"/> object containing the cached update information for the progeny, or <see
        /// langword="null"/> if no cache entry exists for the specified progeny.</returns>
        public TimelineUpdatedCacheEntry GetProgenyOrFamilyTimelineUpdatedCache(int progenyId, int familyId, TimeLineType timelineType)
        {
            if (progenyId > 0)
            {
                string cachedTimelineEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "timelineCacheEntry_p_" + progenyId + "_t_" + (int)timelineType);
                if (!string.IsNullOrEmpty(cachedTimelineEntry))
                {
                    TimelineUpdatedCacheEntry timelineCacheEntry = JsonSerializer.Deserialize<TimelineUpdatedCacheEntry>(cachedTimelineEntry, JsonSerializerOptions.Web);
                    return timelineCacheEntry;
                }
            }

            if (familyId > 0)
            {
                string cachedTimelineEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "timelineCacheEntry_f_" + familyId + "_t_" + (int)timelineType);
                if (!string.IsNullOrEmpty(cachedTimelineEntry))
                {
                    TimelineUpdatedCacheEntry timelineCacheEntry = JsonSerializer.Deserialize<TimelineUpdatedCacheEntry>(cachedTimelineEntry, JsonSerializerOptions.Web);
                    return timelineCacheEntry;
                }
            }
            
            TimelineUpdatedCacheEntry emptyEntry = new()
            {
                ProgenyId = progenyId,
                FamilyId = familyId,
                TimeLineType = timelineType,
                UpdateTime = DateTime.MinValue
            };

            return emptyEntry;
        }

        /// <summary>
        /// Stores the specified item update information in the distributed cache with a sliding expiration of seven
        /// days.
        /// </summary>
        /// <remarks>The cached entry will expire if not accessed within seven days. The cache key is
        /// constructed using the application name, API version, item type, and item ID to ensure uniqueness.</remarks>
        /// <param name="itemType">The type of timeline to which the item belongs. Determines the cache key used for storage.</param>
        /// <param name="itemId">The unique identifier of the item whose update information is to be cached.</param>
        public void SetItemUpdatedCache(TimeLineType itemType, int itemId)
        {
            ItemUpdatedCacheEntry itemCacheEntry = new()
            {
                ItemId = itemId,
                ItemType = itemType,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "itemUpdatedCacheEntry_t_" + (int)itemCacheEntry.ItemType + "_id_" + itemCacheEntry.ItemId
                , JsonSerializer.Serialize(itemCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached update entry for a specific item and timeline type, if available.
        /// </summary>
        /// <param name="itemType">The type of timeline to which the item belongs. Determines the cache key used for retrieval.</param>
        /// <param name="itemId">The unique identifier of the item whose update cache entry is to be retrieved.</param>
        /// <returns>An ItemUpdatedCacheEntry object containing the cached update information for the specified item and timeline
        /// type, or null if no cache entry exists.</returns>
        public ItemUpdatedCacheEntry GetItemUpdatedCache(TimeLineType itemType, int itemId)
        {
            string cachedItemEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "itemUpdatedCacheEntry_t_" + (int)itemType + "_id_" + itemId);
            if (!string.IsNullOrEmpty(cachedItemEntry))
            {
                ItemUpdatedCacheEntry itemCacheEntry = JsonSerializer.Deserialize<ItemUpdatedCacheEntry>(cachedItemEntry, JsonSerializerOptions.Web);
                return itemCacheEntry;
            }

            ItemUpdatedCacheEntry emptyEntry = new()
            {
                ItemId = itemId,
                ItemType = itemType,
                UpdateTime = DateTime.MinValue
            };

            return emptyEntry;
        }

        /// <summary>
        /// Stores the specified list of notes in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached notes list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the notes list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the notes list.</param>
        /// <param name="notesList">The list of notes to cache. Cannot be null.</param>
        public void SetNotesListCache(string userId, int progenyId, List<Note> notesList)
        {
            NotesListCacheEntry notesListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                NotesList = notesList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "notesListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(notesListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached notes list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose notes list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the notes list cache entry is requested.</param>
        /// <returns>A <see cref="NotesListCacheEntry"/> object containing the cached notes list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public NotesListCacheEntry GetNotesListCache(string userId, int progenyId)
        {
            string cachedNotesListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "notesListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesListEntry))
            {
                NotesListCacheEntry notesListCacheEntry = JsonSerializer.Deserialize<NotesListCacheEntry>(cachedNotesListEntry, JsonSerializerOptions.Web);
                return notesListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of pictures in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached pictures list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the pictures list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the pictures list.</param>
        /// <param name="picturesList">The list of pictures to cache. Cannot be null.</param>
        public void SetPicturesListCache(string userId, int progenyId, List<Picture> picturesList)
        {
            PicturesListCacheEntry picturesListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                PicturesList = picturesList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "picturesListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(picturesListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached pictures list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose pictures list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the pictures list cache entry is requested.</param>
        /// <returns>A <see cref="PicturesListCacheEntry"/> object containing the cached pictures list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public PicturesListCacheEntry GetPicturesListCache(string userId, int progenyId)
        {
            string cachedPicturesListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "picturesListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedPicturesListEntry))
            {
                PicturesListCacheEntry picturesListCacheEntry = JsonSerializer.Deserialize<PicturesListCacheEntry>(cachedPicturesListEntry, JsonSerializerOptions.Web);
                return picturesListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of contacts in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached contacts list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the contacts list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the contacts list.</param>
        /// <param name="familyId">The identifier of the family associated with the contacts list.</param>
        /// <param name="contactsList">The list of contacts to cache. Cannot be null.</param>
        public void SetContactsListCache(string userId, int progenyId, int familyId, List<Contact> contactsList)
        {
            ContactsListCacheEntry contactsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                FamilyId = familyId,
                ContactsList = contactsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "contactsListCacheEntry_u_" + userId + "_p_" + progenyId + "_f_" + familyId
                , JsonSerializer.Serialize(contactsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

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
        public ContactsListCacheEntry GetContactsListCache(string userId, int progenyId, int familyId)
        {
            string cachedContactsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "contactsListCacheEntry_u_" + userId + "_p_" + progenyId + "_f_" + familyId);
            if (!string.IsNullOrEmpty(cachedContactsListEntry))
            {
                ContactsListCacheEntry contactsListCacheEntry = JsonSerializer.Deserialize<ContactsListCacheEntry>(cachedContactsListEntry, JsonSerializerOptions.Web);
                return contactsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of friends in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached friends list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the friends list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the friends list.</param>
        /// <param name="friendsList">The list of friends to cache. Cannot be null.</param>
        public void SetFriendsListCache(string userId, int progenyId, List<Friend> friendsList)
        {
            FriendsListCacheEntry friendsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                FriendsList = friendsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "friendsListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(friendsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }
        
        /// <summary>
        /// Retrieves the cached friends list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose friends list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the friends list cache entry is requested.</param>
        /// <returns>A <see cref="FriendsListCacheEntry"/> object containing the cached friends list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public FriendsListCacheEntry GetFriendsListCache(string userId, int progenyId)
        {
            string cachedFriendsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "friendsListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsListEntry))
            {
                FriendsListCacheEntry friendsListCacheEntry = JsonSerializer.Deserialize<FriendsListCacheEntry>(cachedFriendsListEntry, JsonSerializerOptions.Web);
                return friendsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of locations in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached locations list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the locations list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the locations list.</param>
        /// <param name="familyId">The identifier of the family associated with the locations list.</param>
        /// <param name="locationsList">The list of locations to cache. Cannot be null.</param>
        public void SetLocationsListCache(string userId, int progenyId, int familyId, List<Location> locationsList)
        {
            LocationsListCacheEntry locationsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                FamilyId = familyId,
                LocationsList = locationsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "locationsListCacheEntry_u_" + userId + "_p_" + progenyId + "_f_" + familyId
                , JsonSerializer.Serialize(locationsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

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
        public LocationsListCacheEntry GetLocationsListCache(string userId, int progenyId, int familyId)
        {
            string cachedLocationsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "locationsListCacheEntry_u_" + userId + "_p_" + progenyId + "_f_" + familyId);
            if (!string.IsNullOrEmpty(cachedLocationsListEntry))
            {
                LocationsListCacheEntry locationsListCacheEntry = JsonSerializer.Deserialize<LocationsListCacheEntry>(cachedLocationsListEntry, JsonSerializerOptions.Web);
                return locationsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of measurements in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached measurements list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the measurements list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the measurements list.</param>
        /// <param name="measurementsList">The list of measurements to cache. Cannot be null.</param>
        public void SetMeasurementsListCache(string userId, int progenyId, List<Measurement> measurementsList)
        {
            MeasurementsListCacheEntry measurementsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                MeasurementsList = measurementsList,
                UpdateTime = DateTime.UtcNow
            };
            
            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "measurementsListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(measurementsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached measurements list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose measurements list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the measurements list cache entry is requested.</param>
        /// <returns>A <see cref="MeasurementsListCacheEntry"/> object containing the cached measurements list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public MeasurementsListCacheEntry GetMeasurementsListCache(string userId, int progenyId)
        {
            string cachedMeasurementsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "measurementsListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsListEntry))
            {
                MeasurementsListCacheEntry measurementsListCacheEntry = JsonSerializer.Deserialize<MeasurementsListCacheEntry>(cachedMeasurementsListEntry, JsonSerializerOptions.Web);
                return measurementsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of skills in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached skills list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the skills list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the skills list.</param>
        /// <param name="skillsList">The list of skills to cache. Cannot be null.</param>
        public void SetSkillsListCache(string userId, int progenyId, List<Skill> skillsList)
        {
            SkillsListCacheEntry skillsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                SkillsList = skillsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "skillsListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(skillsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached skills list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose skills list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the skills list cache entry is requested.</param>
        /// <returns>A <see cref="SkillsListCacheEntry"/> object containing the cached skills list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public SkillsListCacheEntry GetSkillsListCache(string userId, int progenyId)
        {
            string cachedSkillsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "skillsListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsListEntry))
            {
                SkillsListCacheEntry skillsListCacheEntry = JsonSerializer.Deserialize<SkillsListCacheEntry>(cachedSkillsListEntry, JsonSerializerOptions.Web);
                return skillsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of sleep in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached sleep list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the sleep list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the sleep list.</param>
        /// <param name="sleepList">The list of sleep to cache. Cannot be null.</param>
        public void SetSleepListCache(string userId, int progenyId, List<Sleep> sleepList)
        {
            SleepListCacheEntry sleepListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                SleepList = sleepList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "sleepListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(sleepListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached sleep list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose sleep list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the sleep list cache entry is requested.</param>
        /// <returns>A <see cref="SleepListCacheEntry"/> object containing the cached sleep list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public SleepListCacheEntry GetSleepListCache(string userId, int progenyId)
        {
            string cachedSleepListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "sleepListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepListEntry))
            {
                SleepListCacheEntry sleepListCacheEntry = JsonSerializer.Deserialize<SleepListCacheEntry>(cachedSleepListEntry, JsonSerializerOptions.Web);
                return sleepListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of vaccinations in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached vaccinations list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the vaccinations list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the vaccinations list.</param>
        /// <param name="vaccinationsList">The list of vaccinations to cache. Cannot be null.</param>
        public void SetVaccinationsListCache(string userId, int progenyId, List<Vaccination> vaccinationsList)
        {
            VaccinationsListCacheEntry vaccinationsListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                VaccinationsList = vaccinationsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "vaccinationsListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(vaccinationsListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached vaccinations list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose vaccinations list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the vaccinations list cache entry is requested.</param>
        /// <returns>A <see cref="VaccinationsListCacheEntry"/> object containing the cached vaccinations list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public VaccinationsListCacheEntry GetVaccinationsListCache(string userId, int progenyId)
        {
            string cachedVaccinationsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "vaccinationsListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsListEntry))
            {
                VaccinationsListCacheEntry vaccinationsListCacheEntry = JsonSerializer.Deserialize<VaccinationsListCacheEntry>(cachedVaccinationsListEntry, JsonSerializerOptions.Web);
                return vaccinationsListCacheEntry;
            }
            return null;
        }

        /// <summary>
        /// Stores the specified list of vocabulary items in the distributed cache for the given user and progeny identifiers.
        /// </summary>
        /// <remarks>The cached vocabulary items list is stored with a sliding expiration of 7 days. Subsequent
        /// accesses to the cache entry will reset the expiration period.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the vocabulary items list is being cached. Cannot be null.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the vocabulary items list.</param>
        /// <param name="vocabularyItemsList">The list of vocabulary items to cache. Cannot be null.</param>
        public void SetVocabularyItemsListCache(string userId, int progenyId, List<VocabularyItem> vocabularyItemsList)
        {
            VocabularyListCacheEntry vocabularyListCacheEntry = new()
            {
                UserId = userId,
                ProgenyId = progenyId,
                VocabularyList = vocabularyItemsList,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0));
            cache.SetString(Constants.AppName + Constants.ApiVersion + "vocabularyListCacheEntry_u_" + userId + "_p_" + progenyId
                , JsonSerializer.Serialize(vocabularyListCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);
        }

        /// <summary>
        /// Retrieves the cached vocabulary items list entry for the specified user and progeny identifiers.
        /// </summary>
        /// <remarks>Returns a cached result if available; otherwise, returns null. The cache key is based
        /// on the combination of user and progeny identifiers.</remarks>
        /// <param name="userId">The unique identifier of the user whose vocabulary items list cache entry is to be retrieved. Cannot be null or empty.</param>
        /// <param name="progenyId">The identifier of the progeny for which the vocabulary items list cache entry is requested.</param>
        /// <returns>A <see cref="VocabularyListCacheEntry"/> object containing the cached vocabulary items list entry if found; otherwise, <see
        /// langword="null"/>.</returns>
        public VocabularyListCacheEntry GetVocabularyItemsListCache(string userId, int progenyId)
        {
            string cachedVocabularyItemsListEntry = cache.GetString(Constants.AppName + Constants.ApiVersion + "vocabularyListCacheEntry_u_" + userId + "_p_" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyItemsListEntry))
            {
                VocabularyListCacheEntry vocabularyListCacheEntry = JsonSerializer.Deserialize<VocabularyListCacheEntry>(cachedVocabularyItemsListEntry, JsonSerializerOptions.Web);
                return vocabularyListCacheEntry;
            }
            return null;
        }
    }
}
