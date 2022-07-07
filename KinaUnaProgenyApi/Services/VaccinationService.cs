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
    public class VaccinationService: IVaccinationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public VaccinationService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Vaccination> GetVaccination(int id)
        {
            Vaccination vaccination;
            string cachedVaccination = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }
            else
            {
                vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);
            }

            return vaccination;
        }

        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            _ = _context.VaccinationsDb.Add(vaccination);
            _ = await _context.SaveChangesAsync();
            _ = await SetVaccination(vaccination.VaccinationId);
            return vaccination;
        }

        public async Task<Vaccination> SetVaccination(int id)
        {
            Vaccination vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (vaccination != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);

                List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == vaccination.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + vaccination.ProgenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
            }

            return vaccination;
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            _ = _context.VaccinationsDb.Update(vaccination);
            _ = await _context.SaveChangesAsync();
            _ = await SetVaccination(vaccination.VaccinationId);
            return vaccination;
        }

        public async Task<Vaccination> DeleteVaccination(Vaccination vaccination)
        {
            _ = _context.VaccinationsDb.Remove(vaccination);
            _ = await _context.SaveChangesAsync();
            await RemoveVaccination(vaccination.VaccinationId, vaccination.ProgenyId);

            return vaccination;
        }
        public async Task RemoveVaccination(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);

            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList;
            string cachedVaccinationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }
            else
            {
                vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
            }

            return vaccinationsList;
        }
    }
}
