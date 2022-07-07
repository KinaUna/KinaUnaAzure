using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ISkillService
    {
        Task<Skill> GetSkill(int id);
        Task<Skill> AddSkill(Skill skill);
        Task<Skill> SetSkill(int id);
        Task<Skill> UpdateSkill(Skill skill);
        Task<Skill> DeleteSkill(Skill skill);
        Task RemoveSkill(int id, int progenyId);
        Task<List<Skill>> GetSkillsList(int progenyId);
    }
}
