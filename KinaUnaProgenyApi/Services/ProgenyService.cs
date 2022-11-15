using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class ProgenyService: IProgenyService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public ProgenyService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        
        public async Task<Progeny> GetProgeny(int id)
        {
            Progeny progeny = await GetProgenyFromCache(id);
            if (progeny == null || progeny.Id == 0)
            {
                progeny = await SetProgenyInCache(id);
            }
            
            return progeny;
        }
        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            await _context.ProgenyDb.AddAsync(progeny);
            await _context.SaveChangesAsync();
            
            await SetProgenyInCache(progeny.Id);

            return progeny;
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            Progeny progenyToUpdate = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToUpdate != null)
            {
                progenyToUpdate.Admins = progeny.Admins;
                progenyToUpdate.BirthDay = progeny.BirthDay;
                progenyToUpdate.Name = progeny.Name;
                progenyToUpdate.NickName = progeny.NickName;
                progenyToUpdate.PictureLink = progeny.PictureLink;
                progenyToUpdate.TimeZone = progeny.TimeZone;

                _context.ProgenyDb.Update(progenyToUpdate);
                await _context.SaveChangesAsync();

                await SetProgenyInCache(progeny.Id);
            }
            
            return progenyToUpdate;
        }

        public async Task<Progeny> DeleteProgeny(Progeny progeny)
        {
            await RemoveProgenyFromCache(progeny.Id);

            Progeny progenyToDelete = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToDelete != null)
            {
                _context.ProgenyDb.Remove(progenyToDelete);
                await _context.SaveChangesAsync();

            }

            return progenyToDelete;
        }

        private async Task<Progeny> GetProgenyFromCache(int id)
        {
            Progeny progeny = new Progeny();
            string cachedProgeny = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }

            return progeny;
        }

        private async Task<Progeny> SetProgenyInCache(int id)
        {
            Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (progeny != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            }

            return progeny;
        }

        private async Task RemoveProgenyFromCache(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
        }
    }
}
