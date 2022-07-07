using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class LocationService: ILocationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public LocationService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Location> GetLocation(int id)
        {
            Location location;
            string cachedLocation = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }
            else
            {
                location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);
            }

            return location;
        }

        public async Task<Location> AddLocation(Location location)
        {
            _ = _context.LocationsDb.Add(location);
            _ = await _context.SaveChangesAsync();
            _ = await SetLocation(location.LocationId);
            return location;
        }

        public async Task<Location> SetLocation(int id)
        {
            Location location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            if (location != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

                List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == location.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + location.ProgenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return location;
        }

        public async Task<Location> UpdateLocation(Location location)
        {
            _ = _context.LocationsDb.Update(location);
            _ = await _context.SaveChangesAsync();
            _ = await SetLocation(location.LocationId);
            return location;
        }

        public async Task<Location> DeleteLocation(Location location)
        {
            await RemoveLocation(location.LocationId, location.ProgenyId);

            _context.LocationsDb.Remove(location);
            await _context.SaveChangesAsync();
            
            return location;
        }
        public async Task RemoveLocation(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "location" + id);

            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
        }

        public async Task<List<Location>> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return locationsList;
        }

        public async Task<Address> GetAddressItem(int id)
        {
            Address address;
            string cachedAddress = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }
            else
            {
                address = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(address), _cacheOptionsSliding);

            }

            return address;
        }

        public async Task<Address> SetAddressItem(int id)
        {
            Address addressItem = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);
            
            return addressItem;
        }

        public async Task<Address> AddAddressItem(Address addressItem)
        {
            _ = await _context.AddressDb.AddAsync(addressItem);
            _ = await _context.SaveChangesAsync();

            _ = await SetAddressItem(addressItem.AddressId);

            return addressItem;
        }

        public async Task<Address> UpdateAddressItem(Address addressItem)
        {
            _ = _context.AddressDb.Update(addressItem);
            _ = await _context.SaveChangesAsync();

            _ = await SetAddressItem(addressItem.AddressId);

            return addressItem;
        }

        public async Task RemoveAddressItem(int id)
        {
            Address addressItem = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            if (addressItem != null)
            {
                _ = _context.AddressDb.Remove(addressItem);
                _ = await _context.SaveChangesAsync();

                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
            }
            
        }
    }
}
