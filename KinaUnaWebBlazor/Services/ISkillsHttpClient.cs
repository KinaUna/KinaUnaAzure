using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface ISkillsHttpClient
    {
        /// <summary>
        /// Gets the Skill with the given SkillId.
        /// </summary>
        /// <param name="skillId">int: The Id of the Skill (Skill.SkillId).</param>
        /// <returns>Skill: The Skill object.</returns>
        Task<Skill?> GetSkill(int skillId);

        /// <summary>
        /// Adds a new Skill.
        /// </summary>
        /// <param name="skill">Skill: The new Skill to add.</param>
        /// <returns>Skill</returns>
        Task<Skill?> AddSkill(Skill? skill);

        /// <summary>
        /// Updates a Skill. The Skill with the same SkillId will be updated.
        /// </summary>
        /// <param name="skill">Skill: The Skill to update.</param>
        /// <returns>Skill: The updated Skill object.</returns>
        Task<Skill?> UpdateSkill(Skill? skill);

        /// <summary>
        /// Removes the Skill with a given SkillId.
        /// </summary>
        /// <param name="skillId">int: The Id of the Skill to remove (Skill.SkillId).</param>
        /// <returns>bool: True if the Skill was successfully removed.</returns>
        Task<bool> DeleteSkill(int skillId);

        /// <summary>
        /// Gets a progeny's list of Skills that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Skill objects.</returns>
        Task<List<Skill>?> GetSkillsList(int progenyId, int accessLevel);
    }
}
