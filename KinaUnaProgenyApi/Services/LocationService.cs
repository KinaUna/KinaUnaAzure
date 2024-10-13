using System;
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
    public class LocationService : ILocationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public LocationService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Location by LocationId.
        /// First tries to get the Location from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The LocationId of the Location to get.</param>
        /// <returns>The Location with the given LocationId. Null if the Location doesn't exist.</returns>
        public async Task<Location> GetLocation(int id)
        {
            Location location = await GetLocationFromCache(id);
            if (location == null || location.LocationId == 0)
            {
                location = await SetLocationInCache(id);
            }

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

            Location location = JsonConvert.DeserializeObject<Location>(cachedLocation);
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

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

            _ = await SetLocationsListInCache(location.ProgenyId);

            return location;
        }

        /// <summary>
        /// Adds a new Location to the database and the cache.
        /// </summary>
        /// <param name="location">The Location object to add.</param>
        /// <returns>The added Location object.</returns>
        public async Task<Location> AddLocation(Location location)
        {
            Location locationToAdd = new();
            locationToAdd.CopyPropertiesForAdd(location);

            _ = _context.LocationsDb.Add(locationToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetLocationInCache(locationToAdd.LocationId);

            return locationToAdd;
        }

        /// <summary>
        /// Updates a Location in the database and the cache.
        /// </summary>
        /// <param name="location">Location object with the updated properties.</param>
        /// <returns>The updated Location object.</returns>
        public async Task<Location> UpdateLocation(Location location)
        {
            Location locationToUpdate = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == location.LocationId);
            if (locationToUpdate == null) return null;

            locationToUpdate.CopyPropertiesForUpdate(location);

            _ = _context.LocationsDb.Update(locationToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetLocationInCache(locationToUpdate.LocationId);

            return locationToUpdate;
        }

        /// <summary>
        /// Deletes a Location from the database and the cache.
        /// Then deletes the Location from the LocationsList cached for the Progeny.
        /// </summary>
        /// <param name="location">The Location to delete.</param>
        /// <returns>The deleted Location object.</returns>
        public async Task<Location> DeleteLocation(Location location)
        {
            Location locationToDelete = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == location.LocationId);
            if (locationToDelete == null) return null;

            _ = _context.LocationsDb.Remove(locationToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveLocationFromCache(location.LocationId, location.ProgenyId);

            return location;
        }

        /// <summary>
        /// Removes a Location from the cache and updates the LocationsList for the Progeny.
        /// </summary>
        /// <param name="id">The LocationId of the Location to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Location belongs to.</param>
        /// <returns></returns>
        private async Task RemoveLocationFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "location" + id);

            _ = await SetLocationsListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="accessLevel">The access level of the user.</param>
        /// <returns>List of Locations.</returns>
        public async Task<List<Location>> GetLocationsList(int progenyId, int accessLevel)
        {
            List<Location> locationsList = await GetLocationsListFromCache(progenyId);
            if (locationsList.Count == 0)
            {
                locationsList = await SetLocationsListInCache(progenyId);
            }

            locationsList = locationsList.Where(p => p.AccessLevel >= accessLevel).ToList();
            return locationsList;
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <returns>List of Locations.</returns>
        private async Task<List<Location>> GetLocationsListFromCache(int progenyId)
        {
            List<Location> locationsList = [];
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }

            return locationsList;
        }

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <returns>List of Locations.</returns>
        private async Task<List<Location>> SetLocationsListInCache(int progenyId)
        {
            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);

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

            Address address = JsonConvert.DeserializeObject<Address>(cachedAddress);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);

            return addressItem;
        }

        /// <summary>
        /// Adds a new Address to the database and the cache.
        /// </summary>
        /// <param name="addressItem">The Address object to add.</param>
        /// <returns>The added Address object.</returns>
        public async Task<Address> AddAddressItem(Address addressItem)
        {
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

        public async Task<List<Location>> GetLocationsWithTag(int progenyId, string tag, int accessLevel)
        {
            List<Location> allItems = await GetLocationsList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(l => l.Tags != null && l.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }
    }
}
