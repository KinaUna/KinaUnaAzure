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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Location = KinaUna.Data.Models.Location;

namespace KinaUnaProgenyApi.Services
{
    public class LocationService : ILocationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public LocationService(ProgenyDbContext context, IDistributedCache cache, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Location by LocationId.
        /// First tries to get the Location from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The LocationId of the Location to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Location with the given LocationId. Null if the Location doesn't exist.</returns>
        public async Task<Location> GetLocation(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Location location = await GetLocationFromCache(id);
            if (location == null || location.LocationId == 0)
            {
                location = await SetLocationInCache(id);
            }
            if (location == null || location.LocationId == 0)
            {
                return null;
            }

            location.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, location.LocationId, location.ProgenyId, location.FamilyId, currentUserInfo);
            return location;
        }

        /// <summary>
        /// Gets a Location by LocationId from the cache.
        /// </summary>
        /// <param name="id">The LocationId of the Location to get.</param>
        /// <returns>The Location with the given LocationId. Null if the Location is not found.</returns>
        private async Task<Location> GetLocationFromCache(int id)
        {
            string cachedLocation = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id);
            if (string.IsNullOrEmpty(cachedLocation))
            {
                return null;
            }

            Location location = JsonSerializer.Deserialize<Location>(cachedLocation, JsonSerializerOptions.Web);
            return location;
        }

        /// <summary>
        /// Gets a Location by LocationId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The LocationId of the Location to get and set.</param>
        /// <returns>Location with the given LocationId. Null if the Location doesn't exist.</returns>
        private async Task<Location> SetLocationInCache(int id)
        {
            Location location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            if (location == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonSerializer.Serialize(location, JsonSerializerOptions.Web), _cacheOptionsSliding);

            _ = await SetLocationsListInCache(location.ProgenyId, location.FamilyId);

            return location;
        }

        /// <summary>
        /// Adds a new Location to the database and the cache.
        /// </summary>
        /// <param name="location">The Location object to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The added Location object.</returns>
        public async Task<Location> AddLocation(Location location, UserInfo currentUserInfo)
        {
            // Either ProgenyId or FamilyId must be set, but not both.
            if (location.ProgenyId > 0 && location.FamilyId > 0)
            {
                return null;
            }

            if (location.ProgenyId == 0 && location.FamilyId == 0)
            {
                return null;
            }

            if (location.ProgenyId > 0)
            {
                if (!await _accessManagementService.HasProgenyPermission(location.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    return null;
                }
            }

            if (location.FamilyId > 0)
            {
                if (!await _accessManagementService.HasFamilyPermission(location.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    return null;
                }
            }

            Location locationToAdd = new();
            locationToAdd.CopyPropertiesForAdd(location);

            _ = _context.LocationsDb.Add(locationToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Location, locationToAdd.LocationId, locationToAdd.ProgenyId, locationToAdd.FamilyId, locationToAdd.ItemPermissionsDtoList,
                currentUserInfo);

            _ = await SetLocationInCache(locationToAdd.LocationId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(locationToAdd.ProgenyId, locationToAdd.FamilyId, KinaUnaTypes.TimeLineType.Location);

            return locationToAdd;
        }

        /// <summary>
        /// Updates a Location in the database and the cache.
        /// </summary>
        /// <param name="location">Location object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Location object.</returns>
        public async Task<Location> UpdateLocation(Location location, UserInfo currentUserInfo)
        {
            // Either ProgenyId or FamilyId must be set, but not both.
            if (location.ProgenyId > 0 && location.FamilyId > 0)
            {
                return null;
            }

            if (location.ProgenyId == 0 && location.FamilyId == 0)
            {
                return null;
            }

            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, location.LocationId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Location locationToUpdate = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == location.LocationId);
            if (locationToUpdate == null) return null;

            locationToUpdate.CopyPropertiesForUpdate(location);

            _ = _context.LocationsDb.Update(locationToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Location, locationToUpdate.LocationId, locationToUpdate.ProgenyId, locationToUpdate.FamilyId, locationToUpdate.ItemPermissionsDtoList,
                currentUserInfo);
            _ = await SetLocationInCache(locationToUpdate.LocationId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(locationToUpdate.ProgenyId, locationToUpdate.FamilyId, KinaUnaTypes.TimeLineType.Location);

            return locationToUpdate;
        }

        /// <summary>
        /// Deletes a Location from the database and the cache.
        /// Then deletes the Location from the LocationsList cached for the Progeny.
        /// </summary>
        /// <param name="location">The Location to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Location object.</returns>
        public async Task<Location> DeleteLocation(Location location, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, location.LocationId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Location locationToDelete = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == location.LocationId);
            if (locationToDelete == null) return null;

            _ = _context.LocationsDb.Remove(locationToDelete);
            _ = await _context.SaveChangesAsync();

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, locationToDelete.LocationId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await RemoveLocationFromCache(location.LocationId, location.ProgenyId, location.FamilyId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(locationToDelete.ProgenyId, locationToDelete.FamilyId, KinaUnaTypes.TimeLineType.Location);
            return location;
        }

        /// <summary>
        /// Removes a Location from the cache and updates the LocationsList for the Progeny.
        /// </summary>
        /// <param name="id">The LocationId of the Location to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Location belongs to.</param>
        /// <param name="familyId"></param>
        /// <returns></returns>
        private async Task RemoveLocationFromCache(int id, int progenyId, int familyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "location" + id);

            _ = await SetLocationsListInCache(progenyId, familyId);
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="familyId"></param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Locations.</returns>
        public async Task<List<Location>> GetLocationsList(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            LocationsListCacheEntry cacheEntry = await _kinaUnaCacheService.GetLocationsListCache(currentUserInfo.UserId, progenyId, familyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = await _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, familyId, KinaUnaTypes.TimeLineType.Location);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.LocationsList.ToList();
                }
            }

            Location[] locationsList = await GetLocationsListFromCache(progenyId, familyId);
            if (locationsList.Length == 0)
            {
                locationsList = await SetLocationsListInCache(progenyId, familyId);
            }

            List<Location> accessibleLocations = [];
            foreach (Location location in locationsList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, location.LocationId, currentUserInfo, PermissionLevel.View))
                {
                    //location.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Location, location.LocationId, progenyId, familyId, currentUserInfo);
                    accessibleLocations.Add(location);
                }
            }

            await _kinaUnaCacheService.SetLocationsListCache(currentUserInfo.UserId, progenyId, familyId, accessibleLocations.ToArray());

            return accessibleLocations;
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="familyId"></param>
        /// <returns>List of Locations.</returns>
        private async Task<Location[]> GetLocationsListFromCache(int progenyId, int familyId)
        {
            Location[] locationsList = [];
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId + "_family_" + familyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonSerializer.Deserialize<Location[]>(cachedLocationsList, JsonSerializerOptions.Web);
            }

            return locationsList;
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="familyId"></param>
        /// <returns>List of Locations.</returns>
        private async Task<Location[]> SetLocationsListInCache(int progenyId, int familyId)
        {
            Location[] locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId && l.FamilyId == familyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId + "_family_" + familyId, JsonSerializer.Serialize(locationsList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return locationsList;
        }

        /// <summary>
        /// Gets an Address by AddressId.
        /// First tries to get the Address from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address item to get.</param>
        /// <returns>Address object with the given AddressId.</returns>
        public async Task<Address> GetAddressItem(int id)
        {
            // Todo: Permission check?
            Address address = await GetAddressFromCache(id);
            if (address == null || address.AddressId == 0)
            {
                address = await SetAddressItemInCache(id);
            }

            return address;
        }

        /// <summary>
        /// Gets an Address by AddressId from the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address entity to get.</param>
        /// <returns>Address object with the given AddressId. Null if the Address isn't found.</returns>
        private async Task<Address> GetAddressFromCache(int id)
        {
            string cachedAddress = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
            if (string.IsNullOrEmpty(cachedAddress))
            {
                return null;
            }

            Address address = JsonSerializer.Deserialize<Address>(cachedAddress, JsonSerializerOptions.Web);
            return address;
        }

        /// <summary>
        /// Gets an Address by AddressId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address to get and set.</param>
        /// <returns>Address object with the given AddressId. Null if the Address entity doesn't exist.</returns>
        private async Task<Address> SetAddressItemInCache(int id)
        {
            Address addressItem = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonSerializer.Serialize(addressItem, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return addressItem;
        }

        /// <summary>
        /// Adds a new Address to the database and the cache.
        /// </summary>
        /// <param name="addressItem">The Address object to add.</param>
        /// <returns>The added Address object.</returns>
        public async Task<Address> AddAddressItem(Address addressItem)
        {
            // Todo: Permission check?
            Address addressToAdd = new();
            addressToAdd.CopyPropertiesForAdd(addressItem);
            _ = _context.AddressDb.Add(addressToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetAddressItemInCache(addressToAdd.AddressId);

            return addressToAdd;
        }

        /// <summary>
        /// Updates an Address in the database and the cache.
        /// </summary>
        /// <param name="addressItem">The Address object with the updated properties.</param>
        /// <returns>The updated Address object.</returns>
        public async Task<Address> UpdateAddressItem(Address addressItem)
        {
            // Todo: Permission check?
            Address addressToUpdate = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == addressItem.AddressId);
            if (addressToUpdate == null) return null;

            addressToUpdate.CopyPropertiesForUpdate(addressItem);

            _ = _context.AddressDb.Update(addressToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetAddressItemInCache(addressItem.AddressId);

            return addressToUpdate;
        }

        /// <summary>
        /// Deletes an Address from the database and the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address to delete.</param>
        /// <returns></returns>
        public async Task RemoveAddressItem(int id)
        {
            // Todo: Permission check?
            Address addressToRemove = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            if (addressToRemove != null)
            {
                _ = _context.AddressDb.Remove(addressToRemove);
                _ = await _context.SaveChangesAsync();

                await RemoveAddressFromCache(id);
            }
        }

        /// <summary>
        /// Removes an Address from the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address item to remove.</param>
        /// <returns></returns>
        private async Task RemoveAddressFromCache(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
        }

        /// <summary>
        /// Retrieves a list of locations associated with the specified progeny ID, filtered by a tag.
        /// </summary>
        /// <remarks>The method retrieves all locations for the specified progeny and filters them by the
        /// provided tag, if any.  The tag comparison is case-insensitive and culture-aware.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose locations are to be retrieved.</param>
        /// <param name="familyId"></param>
        /// <param name="tag">An optional tag used to filter the locations. If null or empty, all locations are returned.</param>
        /// <param name="currentUserInfo">The user information of the current user, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Location"/>
        /// objects associated with the specified progeny ID, filtered by the specified tag if provided.</returns>
        public async Task<List<Location>> GetLocationsWithTag(int progenyId, int familyId, string tag, UserInfo currentUserInfo)
        {
            List<Location> allItems = await GetLocationsList(progenyId, familyId, currentUserInfo);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(l => l.Tags != null && l.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }
    }
}
