using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the UserAccess API.
    /// </summary>
    public interface IUserAccessHttpClient
    {
        /// <summary>
        /// Gets the UserAccess with a given AccessId.
        /// </summary>
        /// <param name="accessId">The AccessId of the UserAccess.</param>
        /// <returns>The UserAccess with the given AccessId. If not found or an error occurs, a new UserAccess with AccessId = 0.</returns>
        Task<UserAccess> GetUserAccess(int accessId);

        /// <summary>
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">The UserAccess object to be added.</param>
        /// <returns>The UserAccess object that was added. If an error occurs, a new UserAccess with AccessId = 0.</returns>
        Task<UserAccess> AddUserAccess(UserAccess userAccess);

        /// <summary>
        /// Updates a UserAccess object. The UserAccess with the same AccessId will be updated.
        /// </summary>
        /// <param name="userAccess">The UserAccess object with the updated properties.</param>
        /// <returns>The updated UserAccess object. If not found or an error occurs, a new UserAccess with AccessId = 0.</returns>
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);

        /// <summary>
        /// Deletes a UserAccess object.
        /// </summary>
        /// <param name="userAccessId">The UserAccess object's AccessId.</param>
        /// <returns>bool: True if the UserAccess object was successfully deleted.</returns>
        Task<bool> DeleteUserAccess(int userAccessId);

        /// <summary>
        /// Gets the list of UserAccess for a progeny.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetProgenyAccessList(int progenyId);

        /// <summary>
        /// Gets the list of UserAccess for a user.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetUserAccessList(string userEmail);
    }
}
