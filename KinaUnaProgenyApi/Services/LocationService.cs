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
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Location> GetLocation(int id)
        {
            Location location = await GetLocationFromCache(id);
            if (location == null || location.LocationId == 0)
            {
                location = await SetLocationInCache(id);
            }

            return location;
        }

        private async Task<Location> GetLocationFromCache(int id)
        {
            Location location = new();
            string cachedLocation = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }

            return location;
        }

        private async Task<Location> SetLocationInCache(int id)
        {
            Location location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            if (location == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

            _ = await SetLocationsListInCache(location.ProgenyId);

            return location;
        }

        public async Task<Location> AddLocation(Location location)
        {
            Location locationToAdd = new();
            locationToAdd.CopyPropertiesForAdd(location);

            _ = _context.LocationsDb.Add(locationToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetLocationInCache(locationToAdd.LocationId);

            return locationToAdd;
        }

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

        public async Task<Location> DeleteLocation(Location location)
        {
            Location locationToDelete = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == location.LocationId);
            if (locationToDelete == null) return null;

            _ = _context.LocationsDb.Remove(locationToDelete);
            _ = await _context.SaveChangesAsync();

            await RemoveLocationFromCache(location.LocationId, location.ProgenyId);

            return location;
        }

        private async Task RemoveLocationFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "location" + id);

            _ = await SetLocationsListInCache(progenyId);
        }

        public async Task<List<Location>> GetLocationsList(int progenyId)
        {
            List<Location> locationsList = await GetLocationsListFromCache(progenyId);
            if (locationsList.Count == 0)
            {
                locationsList = await SetLocationsListInCache(progenyId);
            }

            return locationsList;
        }

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

        private async Task<List<Location>> SetLocationsListInCache(int progenyId)
        {
            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);

            return locationsList;
        }

        public async Task<Address> GetAddressItem(int id)
        {
            Address address = await GetAddressFromCache(id);
            if (address == null || address.AddressId == 0)
            {
                address = await SetAddressItemInCache(id);
            }

            return address;
        }

        private async Task<Address> GetAddressFromCache(int id)
        {
            Address address = new();
            string cachedAddress = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }

            return address;
        }

        private async Task<Address> SetAddressItemInCache(int id)
        {
            Address addressItem = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);

            return addressItem;
        }

        public async Task<Address> AddAddressItem(Address addressItem)
        {
            Address addressToAdd = new();
            addressToAdd.CopyPropertiesForAdd(addressItem);
            _ = await _context.AddressDb.AddAsync(addressToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetAddressItemInCache(addressToAdd.AddressId);

            return addressToAdd;
        }

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

        private async Task RemoveAddressFromCache(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
        }
    }
}
