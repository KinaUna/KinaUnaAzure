using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IUserAccessHttpClient
    {
        /// <summary>
        /// Gets the UserAccess with a given UserAccess Id.
        /// </summary>
        /// <param name="userAccessId">int: The Id of the UserAccess (UserAccess.AccessId).</param>
        /// <returns>UserAccess</returns>
        Task<UserAccess> GetUserAccess(int userAccessId);

        /// <summary>
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be added.</param>
        /// <returns>UserAccess: The UserAccess object that was added.</returns>
        Task<UserAccess> AddUserAccess(UserAccess userAccess);

        /// <summary>
        /// Updates a UserAccess object. The UserAccess with the same AccessId will be updated.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be updated.</param>
        /// <returns>UserAccess: The updated UserAccess object.</returns>
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);

        /// <summary>
        /// Removes a UserAccess object.
        /// </summary>
        /// <param name="userAccessId">int: The UserAccess object's Id (UserAccess.AccessId).</param>
        /// <returns>bool: True if the UserAccess object was successfully removed.</returns>
        Task<bool> DeleteUserAccess(int userAccessId);

        /// <summary>
        /// Gets the list of UserAccess for a progeny.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetProgenyAccessList(int progenyId);

        /// <summary>
        /// Gets the list of UserAccess for a user.
        /// </summary>
        /// <param name="userEmail">string: The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetUserAccessList(string userEmail);
    }
}
