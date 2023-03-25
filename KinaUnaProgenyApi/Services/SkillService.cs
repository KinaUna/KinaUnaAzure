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
    public class SkillService : ISkillService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public SkillService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Skill> GetSkill(int id)
        {
            Skill skill = await GetSkillFromCache(id);
            if (skill == null || skill.SkillId == 0)
            {
                skill = await SetSkillInCache(id);
            }
            return skill;
        }

        public async Task<Skill> AddSkill(Skill skill)
        {
            Skill skillToAdd = new Skill();
            skillToAdd.CopyPropertiesForAdd(skill);
            _ = _context.SkillsDb.Add(skillToAdd);
            _ = await _context.SaveChangesAsync();
            _ = await SetSkillInCache(skillToAdd.SkillId);

            return skillToAdd;
        }

        private async Task<Skill> GetSkillFromCache(int id)
        {
            Skill skill = new Skill();
            string cachedSkill = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }

            return skill;
        }

        public async Task<Skill> SetSkillInCache(int id)
        {
            Skill skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            if (skill != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

                _ = await SetSkillsListInCache(skill.ProgenyId);
            }

            return skill;
        }

        public async Task<Skill> UpdateSkill(Skill skill)
        {
            Skill skillToUpdate = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == skill.SkillId);
            if (skillToUpdate != null)
            {
                skillToUpdate.CopyPropertiesForUpdate(skill);

                _ = _context.SkillsDb.Update(skillToUpdate);
                _ = await _context.SaveChangesAsync();

                _ = await SetSkillInCache(skill.SkillId);
            }

            return skillToUpdate;
        }

        public async Task<Skill> DeleteSkill(Skill skill)
        {
            Skill skillToDelete = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == skill.SkillId);
            if (skillToDelete != null)
            {
                _ = _context.SkillsDb.Remove(skillToDelete);
                _ = await _context.SaveChangesAsync();
                await RemoveSkillFromCache(skill.SkillId, skill.ProgenyId);
            }

            return skillToDelete;
        }
        public async Task RemoveSkillFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);

            _ = await SetSkillsListInCache(progenyId);
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList = await GetSkillsListFromCache(progenyId);
            if (!skillsList.Any())
            {
                skillsList = await SetSkillsListInCache(progenyId);
            }

            return skillsList;
        }

        private async Task<List<Skill>> GetSkillsListFromCache(int progenyId)
        {
            List<Skill> skillsList = new List<Skill>();
            string cachedSkillsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }

            return skillsList;
        }

        private async Task<List<Skill>> SetSkillsListInCache(int progenyId)
        {
            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);

            return skillsList;
        }
    }
}
