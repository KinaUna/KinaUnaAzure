using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ISkillService
    {
        /// <summary>
        /// Gets the Skill with the specified SkillId.
        /// First checks the cache, if not found, gets the Skill from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to get.</param>
        /// <returns>The Skill object with the given SkillId. Null if it doesn't exist.</returns>
        Task<Skill> GetSkill(int id);

        /// <summary>
        /// Adds a new Skill to the database and adds it to the cache.
        /// </summary>
        /// <param name="skill">The Skill object to add.</param>
        /// <returns>The added Skill object.</returns>
        Task<Skill> AddSkill(Skill skill);

        /// <summary>
        /// Updates a Skill in the database and the cache.
        /// </summary>
        /// <param name="skill">The SKill object with the updated properties.</param>
        /// <returns>The updated Skill object.</returns>
        Task<Skill> UpdateSkill(Skill skill);

        /// <summary>
        /// Deletes a Skill from the database and the cache.
        /// </summary>
        /// <param name="skill">The Skill object to delete.</param>
        /// <returns>The deleted Skill object.</returns>
        Task<Skill> DeleteSkill(Skill skill);

        /// <summary>
        /// Gets a list of all Skills for a Progeny from the cache.
        /// If the list is empty, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <returns>List of Skill objects.</returns>
        Task<List<Skill>> GetSkillsList(int progenyId);
        Task<List<Skill>> GetSkillsWithCategory(int progenyId, string category);
    }
}
