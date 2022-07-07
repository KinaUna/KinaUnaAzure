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
    public class SkillService: ISkillService
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
            Skill skill;
            string cachedSkill = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }
            else
            {
                skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);
            }

            return skill;
        }

        public async Task<Skill> AddSkill(Skill skill)
        {
            _ = _context.SkillsDb.Add(skill);
            _ = await _context.SaveChangesAsync();
            _ = await SetSkill(skill.SkillId);

            return skill;
        }

        public async Task<Skill> SetSkill(int id)
        {
            Skill skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            if (skill != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

                List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == skill.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + skill.ProgenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
            }
            
            return skill;
        }

        public async Task<Skill> UpdateSkill(Skill skill)
        {
            _ = _context.SkillsDb.Update(skill);
            _ = await _context.SaveChangesAsync();
            _ = await SetSkill(skill.SkillId);

            return skill;
        }

        public async Task<Skill> DeleteSkill(Skill skill)
        {
            _ = _context.SkillsDb.Remove(skill);
            _ = await _context.SaveChangesAsync();
            await RemoveSkill(skill.SkillId, skill.ProgenyId);
            return skill;
        }
        public async Task RemoveSkill(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);

            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList;
            string cachedSkillsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }
            else
            {
                skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
            }

            return skillsList;
        }
    }
}
