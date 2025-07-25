using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Skills API.
    /// </summary>
    public interface ISkillsHttpClient
    {
        /// <summary>
        /// Gets the Skill with the given SkillId.
        /// </summary>
        /// <param name="skillId">The SkillId of the Skill to get.</param>
        /// <returns>The Skill object with the given SkillId. If the Skill cannot be found a new Skill object with SkillId is returned.</returns>
        Task<Skill> GetSkill(int skillId);

        /// <summary>
        /// Adds a new Skill.
        /// </summary>
        /// <param name="skill">The new Skill to add.</param>
        /// <returns>The added Skill object.</returns>
        Task<Skill> AddSkill(Skill skill);

        /// <summary>
        /// Updates a Skill. The Skill with the same SkillId will be updated.
        /// </summary>
        /// <param name="skill">The Skill with the updated properties.</param>
        /// <returns>The updated Skill object.</returns>
        Task<Skill> UpdateSkill(Skill skill);

        /// <summary>
        /// Deletes the Skill with a given SkillId.
        /// </summary>
        /// <param name="skillId">The SkillId of the Skill to delete.</param>
        /// <returns>bool: True if the Skill was successfully removed.</returns>
        Task<bool> DeleteSkill(int skillId);

        /// <summary>
        /// Gets a list of all Skills for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to get Skills for.</param>
        /// <returns>List of Skill objects.</returns>
        Task<List<Skill>> GetSkillsList(int progenyId);
    }
}
