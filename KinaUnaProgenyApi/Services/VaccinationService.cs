using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace KinaUnaProgenyApi.Services
{
    public class VaccinationService : IVaccinationService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public VaccinationService(ProgenyDbContext context, IDistributedCache cache, IAccessManagementService accessManagementService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Vaccination entity with the specified VaccinationId.
        /// First checks the cache, if not found, gets the Vaccination from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination entity to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, used for access control.</param>
        /// <returns>The Vaccination object with the given VaccinationId. Null if the Vaccination item doesn't exist.</returns>
        public async Task<Vaccination> GetVaccination(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Vaccination vaccination = await GetVaccinationFromCache(id);
            if (vaccination == null || vaccination.VaccinationId == 0)
            {
                vaccination = await SetVaccinationInCache(id);
            }
            if (vaccination != null && vaccination.VaccinationId != 0)
            {
                vaccination.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, vaccination.VaccinationId, vaccination.ProgenyId, 0, currentUserInfo);
            }

            return vaccination;
        }

        /// <summary>
        /// Adds a new Vaccination entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Vaccination object.</returns>
        public async Task<Vaccination> AddVaccination(Vaccination vaccination, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(vaccination.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            Vaccination vaccinationToAdd = new();
            vaccinationToAdd.CopyPropertiesForAdd(vaccination);

            _ = _context.VaccinationsDb.Add(vaccinationToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, vaccinationToAdd.VaccinationId, vaccinationToAdd.ProgenyId, 0, vaccinationToAdd.ItemPermissionsDtoList,
                currentUserInfo);
            _ = await SetVaccinationInCache(vaccinationToAdd.VaccinationId);

            return vaccinationToAdd;
        }

        /// <summary>
        /// Gets a Vaccination entity with the specified VaccinationId.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination item to get.</param>
        /// <returns>The Vaccination object with the given VaccinationId. Null if the Vaccination item isn't found in the cache.</returns>
        private async Task<Vaccination> GetVaccinationFromCache(int id)
        {
            string cachedVaccination = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);
            if (string.IsNullOrEmpty(cachedVaccination))
            {
                return null;
            }

            Vaccination vaccination = JsonSerializer.Deserialize<Vaccination>(cachedVaccination, JsonSerializerOptions.Web);
            return vaccination;
        }

        /// <summary>
        /// Gets a Vaccination entity from the database by VaccinationId and adds it to the cache.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination item to get and set.</param>
        /// <returns>The Vaccination object with the given VaccinationId. Null if the Vaccination item doesn't exist.</returns>
        private async Task<Vaccination> SetVaccinationInCache(int id)
        {
            Vaccination vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (vaccination == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonSerializer.Serialize(vaccination, JsonSerializerOptions.Web), _cacheOptionsSliding);

            _ = await SetVaccinationListInCache(vaccination.ProgenyId);

            return vaccination;
        }

        /// <summary>
        /// Updates a Vaccination entity in the database and the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Vaccination object.</returns>
        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccination.VaccinationId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Vaccination vaccinationToUpdate = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == vaccination.VaccinationId);
            if (vaccinationToUpdate == null) return null;

            vaccinationToUpdate.CopyPropertiesForUpdate(vaccination);

            _ = _context.VaccinationsDb.Update(vaccinationToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Vaccination, vaccinationToUpdate.VaccinationId, vaccinationToUpdate.ProgenyId, 0, vaccinationToUpdate.ItemPermissionsDtoList,
                currentUserInfo);

            _ = await SetVaccinationInCache(vaccination.VaccinationId);

            return vaccinationToUpdate;
        }

        /// <summary>
        /// Deletes a Vaccination entity from the database and the cache.
        /// </summary>
        /// <param name="vaccination">The Vaccination object to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns></returns>
        public async Task<Vaccination> DeleteVaccination(Vaccination vaccination, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccination.VaccinationId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Vaccination vaccinationToDelete = await _context.VaccinationsDb.SingleOrDefaultAsync(v => v.VaccinationId == vaccination.VaccinationId);
            if (vaccinationToDelete == null) return null;

            _ = _context.VaccinationsDb.Remove(vaccinationToDelete);
            _ = await _context.SaveChangesAsync();

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, vaccinationToDelete.VaccinationId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await RemoveVaccinationFromCache(vaccination.VaccinationId, vaccination.ProgenyId);
            
            return vaccinationToDelete;
        }

        /// <summary>
        /// Deletes a Vaccination entity from the cache.
        /// Also updates the list of all Vaccinations for the Progeny in the cache.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination to delete.</param>
        /// <param name="progenyId"></param>
        /// <returns></returns>
        private async Task RemoveVaccinationFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);

            _ = await SetVaccinationListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Vaccinations for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, used for access control.</param>
        /// <returns>List of Vaccination objects.</returns>
        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId, UserInfo currentUserInfo)
        {
            List<Vaccination> vaccinationsList = await GetVaccinationListFromCache(progenyId);
            if (vaccinationsList.Count == 0)
            {
                vaccinationsList = await SetVaccinationListInCache(progenyId);
            }

            List<Vaccination> accessibleVaccinationsList = [];
            foreach (Vaccination vaccination in vaccinationsList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, vaccination.VaccinationId, currentUserInfo, PermissionLevel.View))
                {
                    vaccination.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, vaccination.VaccinationId, vaccination.ProgenyId, 0, currentUserInfo);
                    accessibleVaccinationsList.Add(vaccination);
                }
            }
            return accessibleVaccinationsList;
        }

        /// <summary>
        /// Gets a list of all Vaccinations for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Vaccinations for.</param>
        /// <returns>List of Vaccination objects.</returns>
        private async Task<List<Vaccination>> GetVaccinationListFromCache(int progenyId)
        {
            List<Vaccination> vaccinationsList = [];
            string cachedVaccinationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonSerializer.Deserialize<List<Vaccination>>(cachedVaccinationsList, JsonSerializerOptions.Web);
            }

            return vaccinationsList;
        }

        /// <summary>
        /// Gets a list of all Vaccinations for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set the list of Vaccinations for.</param>
        /// <returns>List of Vaccination objects.</returns>
        private async Task<List<Vaccination>> SetVaccinationListInCache(int progenyId)
        {
            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonSerializer.Serialize(vaccinationsList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return vaccinationsList;
        }
    }
}
