using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// The progeny http client interface.
    /// Contains the methods for adding, retrieving and updating progeny and user data.
    /// </summary>
    public interface IProgenyHttpClient
    {
        //Task<HttpClient> GetClient();

        /// <summary>
        /// Gets progeny from the progeny's id.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <returns>Progeny</returns>
        Task<Progeny> GetProgeny(int progenyId);

        /// <summary>
        /// Adds a new progeny.
        /// </summary>
        /// <param name="progeny">Progeny: The Progeny object to be added.</param>
        /// <returns>Progeny: The Progeny object that was added.</returns>
        Task<Progeny> AddProgeny(Progeny progeny);

        /// <summary>
        /// Updates a Progeny.
        /// </summary>
        /// <param name="progeny">Progeny: The Progeny object with updated values.</param>
        /// <returns>Progeny: The updated Progeny object.</returns>
        Task<Progeny> UpdateProgeny(Progeny progeny);

        /// <summary>
        /// Removes a progeny.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny to be removed (Progeny.Id).</param>
        /// <returns>bool: True if successfully removed.</returns>
        Task<bool> DeleteProgeny(int progenyId);

        /// <summary>
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">string: The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        Task<List<Progeny>> GetProgenyAdminList(string email);
        
        /// <summary>
        /// Gets the latest 5 posts (progeny time, not added time) for a progeny, that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel);

        /// <summary>
        /// Gets all the posts from today's date last year (progeny time, not added time), that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's id.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel);


    }
}
