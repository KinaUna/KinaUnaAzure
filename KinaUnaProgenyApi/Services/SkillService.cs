using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class SkillService : ISkillService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public SkillService(ProgenyDbContext context, IDistributedCache cache, IAccessManagementService accessManagementService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the Skill with the specified SkillId.
        /// First checks the cache, if not found, gets the Skill from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Skill object with the given SkillId. Null if it doesn't exist.</returns>
        public async Task<Skill> GetSkill(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Skill skill = await GetSkillFromCache(id);
            if (skill == null || skill.SkillId == 0)
            {
                skill = await SetSkillInCache(id);
            }
            if (skill == null || skill.SkillId == 0)
            {
                return null;
            }

            skill.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, skill.SkillId, skill.ProgenyId, 0, currentUserInfo);

            return skill;
        }

        /// <summary>
        /// Adds a new Skill to the database and adds it to the cache.
        /// </summary>
        /// <param name="skill">The Skill object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Skill object.</returns>
        public async Task<Skill> AddSkill(Skill skill, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(skill.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            Skill skillToAdd = new();
            skillToAdd.CopyPropertiesForAdd(skill);
            _ = _context.SkillsDb.Add(skillToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Skill, skillToAdd.SkillId, skillToAdd.ProgenyId, 0, skillToAdd.ItemPermissionsDtoList, currentUserInfo);
            _ = await SetSkillInCache(skillToAdd.SkillId);
            
            return skillToAdd;
        }

        /// <summary>
        /// Gets the Skill with the specified SkillId from the cache.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to get.</param>
        /// <returns>The SKill with the given SKillId. Null if the Skill isn't found in the cache.</returns>
        private async Task<Skill> GetSkillFromCache(int id)
        {
            string cachedSkill = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);
            if (string.IsNullOrEmpty(cachedSkill))
            {
                return null;
            }

            Skill skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            return skill;
        }

        /// <summary>
        /// Gets the Skill with the specified SkillId from the database and adds it to the cache.
        /// Also updates the SkillsList for the Progeny in the cache.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to get and set.</param>
        /// <returns>The SKill with the given SkillId. Null if the Skill doesn't exist.</returns>
        private async Task<Skill> SetSkillInCache(int id)
        {
            Skill skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            if (skill == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

            _ = await SetSkillsListInCache(skill.ProgenyId);

            return skill;
        }

        /// <summary>
        /// Updates a Skill in the database and the cache.
        /// </summary>
        /// <param name="skill">The SKill object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Skill object.</returns>
        public async Task<Skill> UpdateSkill(Skill skill, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, skill.SkillId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Skill skillToUpdate = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == skill.SkillId);
            if (skillToUpdate == null) return null;

            skillToUpdate.CopyPropertiesForUpdate(skill);

            _ = _context.SkillsDb.Update(skillToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Skill, skillToUpdate.SkillId, skillToUpdate.ProgenyId, 0, skill.ItemPermissionsDtoList, currentUserInfo);
            _ = await SetSkillInCache(skill.SkillId);

            return skillToUpdate;
        }

        /// <summary>
        /// Deletes a Skill from the database and the cache.
        /// </summary>
        /// <param name="skill">The Skill object to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Skill object.</returns>
        public async Task<Skill> DeleteSkill(Skill skill, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, skill.SkillId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Skill skillToDelete = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == skill.SkillId);
            if (skillToDelete == null) return null;

            _ = _context.SkillsDb.Remove(skillToDelete);
            _ = await _context.SaveChangesAsync();

            // Todo: Remove associated permissions.\

            await RemoveSkillFromCache(skill.SkillId, skill.ProgenyId);

            return skillToDelete;
        }

        /// <summary>
        /// Removes a Skill from the cache and updates the SkillsList for the Progeny in the cache.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Skill belongs to.</param>
        /// <returns></returns>
        private async Task RemoveSkillFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);

            _ = await SetSkillsListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Skills for a Progeny from the cache.
        /// If the list is empty, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Skill objects.</returns>
        public async Task<List<Skill>> GetSkillsList(int progenyId, UserInfo currentUserInfo)
        {
            List<Skill> skillsList = await GetSkillsListFromCache(progenyId);
            if (skillsList.Count == 0)
            {
                skillsList = await SetSkillsListInCache(progenyId);
            }

            List<Skill> filteredList = [];
            foreach (Skill skill in skillsList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, skill.SkillId, currentUserInfo, PermissionLevel.View))
                {
                    skill.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Skill, skill.SkillId, skill.ProgenyId, 0, currentUserInfo);
                    filteredList.Add(skill);
                }
            }

            return filteredList;
        }

        /// <summary>
        /// Gets a list of all Skills for a Progeny that match the specified category.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <param name="category">The category to filter by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Skill objects that match the specified category.</returns>
        public async Task<List<Skill>> GetSkillsWithCategory(int progenyId, string category, UserInfo currentUserInfo)
        {
            List<Skill> allItems = await GetSkillsList(progenyId, currentUserInfo);
            allItems = [.. allItems.Where(s => s.Category != null && s.Category.Contains(category, StringComparison.CurrentCultureIgnoreCase))];
            
            return allItems;
        }

        /// <summary>
        /// Gets a list of all Skills for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <returns>List of Skill objects.</returns>
        private async Task<List<Skill>> GetSkillsListFromCache(int progenyId)
        {
            List<Skill> skillsList = [];
            string cachedSkillsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }

            return skillsList;
        }

        /// <summary>
        /// Gets a list of all Skills for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <returns>List of SKill objects.</returns>
        private async Task<List<Skill>> SetSkillsListInCache(int progenyId)
        {
            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);

            return skillsList;
        }
    }
}
