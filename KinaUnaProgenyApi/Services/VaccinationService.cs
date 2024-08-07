﻿using System.Collections.Generic;
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
    public class VaccinationService : IVaccinationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public VaccinationService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Vaccination> GetVaccination(int id)
        {
            Vaccination vaccination = await GetVaccinationFromCache(id);
            if (vaccination == null || vaccination.VaccinationId == 0)
            {
                vaccination = await SetVaccinationInCache(id);
            }

            return vaccination;
        }

        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            Vaccination vaccinationToAdd = new();
            vaccinationToAdd.CopyPropertiesForAdd(vaccination);

            _ = _context.VaccinationsDb.Add(vaccinationToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetVaccinationInCache(vaccinationToAdd.VaccinationId);

            return vaccinationToAdd;
        }

        private async Task<Vaccination> GetVaccinationFromCache(int id)
        {
            Vaccination vaccination = new();
            string cachedVaccination = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }

            return vaccination;
        }

        private async Task<Vaccination> SetVaccinationInCache(int id)
        {
            Vaccination vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (vaccination != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);

                _ = await SetVaccinationListInCache(vaccination.ProgenyId);
            }

            return vaccination;
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            Vaccination vaccinationToUpdate = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == vaccination.VaccinationId);
            if (vaccinationToUpdate == null) return null;

            vaccinationToUpdate.CopyPropertiesForUpdate(vaccination);

            _ = _context.VaccinationsDb.Update(vaccinationToUpdate);
            _ = await _context.SaveChangesAsync();
            _ = await SetVaccinationInCache(vaccination.VaccinationId);

            return vaccinationToUpdate;
        }

        public async Task<Vaccination> DeleteVaccination(Vaccination vaccination)
        {
            Vaccination vaccinationToDelete = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == vaccination.VaccinationId);
            if (vaccinationToDelete == null) return null;

            _ = _context.VaccinationsDb.Remove(vaccinationToDelete);
            _ = await _context.SaveChangesAsync();
            await RemoveVaccinationFromCache(vaccination.VaccinationId, vaccination.ProgenyId);

            return vaccinationToDelete;
        }

        private async Task RemoveVaccinationFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);

            _ = await SetVaccinationListInCache(progenyId);
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList = await GetVaccinationListFromCache(progenyId);
            if (vaccinationsList.Count == 0)
            {
                vaccinationsList = await SetVaccinationListInCache(progenyId);
            }

            return vaccinationsList;
        }

        private async Task<List<Vaccination>> GetVaccinationListFromCache(int progenyId)
        {
            List<Vaccination> vaccinationsList = [];
            string cachedVaccinationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }

            return vaccinationsList;
        }

        private async Task<List<Vaccination>> SetVaccinationListInCache(int progenyId)
        {
            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);

            return vaccinationsList;
        }
    }
}
