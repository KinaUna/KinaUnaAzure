using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface ISleepHttpClient
    {
        /// <summary>
        /// Gets a Sleep with a given SleepId.
        /// </summary>
        /// <param name="sleepId">int: The Id of the sleep object (Sleep.SleepId).</param>
        /// <returns>Sleep</returns>
        Task<Sleep?> GetSleepItem(int sleepId);

        /// <summary>
        /// Adds a new Sleep object.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to be added.</param>
        /// <returns>Sleep: The added sleep object.</returns>
        Task<Sleep?> AddSleep(Sleep? sleep);

        /// <summary>
        /// Updates a Sleep object. The Sleep with the same SleepId will be updated.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to update.</param>
        /// <returns>Sleep: The updated Sleep object.</returns>
        Task<Sleep?> UpdateSleep(Sleep? sleep);

        /// <summary>
        /// Removes the Sleep object with a given SleepId.
        /// </summary>
        /// <param name="sleepId">int: The id of the Sleep object to remove (Sleep.SleepId).</param>
        /// <returns>bool: True if the Sleep object was successfully removed.</returns>
        Task<bool> DeleteSleepItem(int sleepId);

        /// <summary>
        /// Gets the List of Sleep objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The access level of the user.</param>
        /// <returns>List of Sleep objects.</returns>
        Task<List<Sleep>?> GetSleepList(int progenyId, int accessLevel);
    }
}
