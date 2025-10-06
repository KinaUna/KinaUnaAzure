using KinaUna.Data.Models.AccessManagement;
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
        /// Retrieves a list of progenies that the currently signed-in user can access based on the specified permission
        /// level.
        /// </summary>
        /// <remarks>This method uses the currently signed-in user's identity to determine access. The
        /// user must be authenticated, and their access token must be valid.</remarks>
        /// <param name="permissionLevel">The level of permission required to access the progenies. This determines which progenies are included in
        /// the result.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Progeny"/>
        /// objects that the user has access to. If no progenies are accessible, an empty list is returned.</returns>
        Task<List<Progeny>> GetProgeniesUserCanAccess(PermissionLevel permissionLevel);

        /// <summary>
        /// Retrieves the list of permissions associated with a specific progeny.
        /// </summary>
        /// <remarks>This method makes an HTTP request to an external service to retrieve the permissions.
        /// Ensure that the signed-in user has a valid token, as it is required for authentication.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose permissions are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="ProgenyPermission"/> objects representing the permissions for the specified progeny. Returns an empty
        /// list if no permissions are found or if the request fails.</returns>
        Task<List<ProgenyPermission>> GetProgenyPermissionsList(int progenyId);

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
        
    }
}
