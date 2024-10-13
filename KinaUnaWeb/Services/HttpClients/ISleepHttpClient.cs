using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Sleep API.
    /// </summary>
    public interface ISleepHttpClient
    {
        /// <summary>
        /// Gets a Sleep with a given SleepId.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep to get.</param>
        /// <returns>The Sleep object with the given SleepId. If the item cannot be found a new Sleep object with SleepId=0 is returned.</returns>
        Task<Sleep> GetSleepItem(int sleepId);

        /// <summary>
        /// Adds a new Sleep object.
        /// </summary>
        /// <param name="sleep">The Sleep object to be added.</param>
        /// <returns>The added Sleep object.</returns>
        Task<Sleep> AddSleep(Sleep sleep);

        /// <summary>
        /// Updates a Sleep object. The Sleep with the same SleepId will be updated.
        /// </summary>
        /// <param name="sleep">The Sleep object with the updated properties.</param>
        /// <returns>The updated Sleep object.</returns>
        Task<Sleep> UpdateSleep(Sleep sleep);

        /// <summary>
        /// Deletes the Sleep object with a given SleepId.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep object to delete.</param>
        /// <returns>bool: True if the Sleep object was successfully deleted.</returns>
        Task<bool> DeleteSleepItem(int sleepId);

        /// <summary>
        /// Gets the List of all Sleep objects for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <returns>List of Sleep objects.</returns>
        Task<List<Sleep>> GetSleepList(int progenyId);
    }
}
