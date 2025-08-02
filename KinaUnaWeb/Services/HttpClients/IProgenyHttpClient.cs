using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Progeny API.
    /// Contains the methods for adding, retrieving and updating progeny and user data.
    /// </summary>
    public interface IProgenyHttpClient
    {
        /// <summary>
        /// Gets the Progeny with the given Id.
        /// </summary>
        /// <param name="progenyId">The Progeny's Id.</param>
        /// <returns>Progeny object with the given Id. If not found, a new Progeny object with Id=0 is returned.</returns>
        Task<Progeny> GetProgeny(int progenyId);

        /// <summary>
        /// Adds a new Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object to be added.</param>
        /// <returns>Progeny: The Progeny object that was added.</returns>
        Task<Progeny> AddProgeny(Progeny progeny);

        /// <summary>
        /// Updates a Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object with the updated properties.</param>
        /// <returns>The updated Progeny object.</returns>
        Task<Progeny> UpdateProgeny(Progeny progeny);

        /// <summary>
        /// Removes a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to be removed.</param>
        /// <returns>bool: True if successfully removed.</returns>
        Task<bool> DeleteProgeny(int progenyId);

        /// <summary>
        /// Gets the ProgenyInfo object for the given Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get the ProgenyInfo object for.</param>
        /// <returns>The ProgenyInfo object for the given Progeny.</returns>
        Task<ProgenyInfo> GetProgenyInfo(int progenyId);

        /// <summary>
        /// Updates a ProgenyInfo object.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object with the updated properties.</param>
        /// <returns>The updated ProgenyInfo object.</returns>
        Task<ProgenyInfo> UpdateProgenyInfo(ProgenyInfo progenyInfo);
        
        /// <summary>
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        Task<List<Progeny>> GetProgenyAdminList(string email);

        /// <summary>
        /// Gets the list of TimeLineItems that happened on this data for the given Progenies.
        /// </summary>
        /// <param name="progeniesList">List of Ids for the progenies to get timeline items for.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgeniesYearAgo(List<int> progeniesList);
    }
}
