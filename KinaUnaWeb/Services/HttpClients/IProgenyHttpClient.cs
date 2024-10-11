using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

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
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        Task<List<Progeny>> GetProgenyAdminList(string email);

        /// <summary>
        /// Gets the latest 5 posts (timeline time, not added time) for a Progeny, that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel);

        /// <summary>
        /// Gets all the posts with today's day of month and month, for all years (timeline time, not added time), that the user has access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel);

        Task<List<TimeLineItem>> GetProgeniesYearAgo(List<int> progeniesList);
    }
}
