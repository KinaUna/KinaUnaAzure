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
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Skill object with the given SkillId. Null if it doesn't exist.</returns>
        Task<Skill> GetSkill(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Skill to the database and adds it to the cache.
        /// </summary>
        /// <param name="skill">The Skill object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Skill object.</returns>
        Task<Skill> AddSkill(Skill skill, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Skill in the database and the cache.
        /// </summary>
        /// <param name="skill">The SKill object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Skill object.</returns>
        Task<Skill> UpdateSkill(Skill skill, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Skill from the database and the cache.
        /// </summary>
        /// <param name="skill">The Skill object to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Skill object.</returns>
        Task<Skill> DeleteSkill(Skill skill, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Skills for a Progeny from the cache.
        /// If the list is empty, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Skill objects.</returns>
        Task<List<Skill>> GetSkillsList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Skills for a Progeny that match the specified category.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Skills for.</param>
        /// <param name="category">The category to filter by.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Skill objects that match the specified category.</returns>
        Task<List<Skill>> GetSkillsWithCategory(int progenyId, string category, UserInfo currentUserInfo);
    }
}
