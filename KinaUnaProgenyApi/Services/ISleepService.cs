using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ISleepService
    {
        /// <summary>
        /// Gets the Sleep with the specified SleepId.
        /// First checks the cache, if not found, gets the Sleep from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep to get.</param>
        /// <returns>The Sleep object with the given SleepId. Null if the Sleep item doesn't exist.</returns>
        Task<Sleep> GetSleep(int id);

        /// <summary>
        /// Adds a new Sleep to the database and adds it to the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to add.</param>
        /// <returns>The added Sleep object.</returns>
        Task<Sleep> AddSleep(Sleep sleep);

        /// <summary>
        /// Updates a Sleep in the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object with the updated properties.</param>
        /// <returns>The updated Sleep object.</returns>
        Task<Sleep> UpdateSleep(Sleep sleep);

        /// <summary>
        /// Deletes a Sleep from the database and the cache.
        /// </summary>
        /// <param name="sleep">The Sleep object to delete.</param>
        /// <returns>The deleted Sleep object.</returns>
        Task<Sleep> DeleteSleep(Sleep sleep);

        /// <summary>
        /// Gets a list of all Sleep items for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep items for.</param>
        /// <param name="accessLevel">The access level for the current user.</param>
        /// <returns>List of Sleep objects.</returns>
        Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel);
    }
}
