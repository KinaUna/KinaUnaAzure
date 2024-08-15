using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserAccessService
    {
        /// <summary>
        /// Gets the list of Progeny where the user is an admin.
        /// Gets the list from the cache if it exists, otherwise gets the list from the database and adds it to the cache.
        /// The reason email is used instead of UserId is that access for a user can be granted by email address, so that even if a user hasn't created an account yet, they can still be granted access as soon as they do.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>List of Progeny objects.</returns>
        Task<List<Progeny>> GetProgenyUserIsAdmin(string email);

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of UserAccesses for.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId);

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a user.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetUsersUserAccessList(string email);

        /// <summary>
        /// Gets the list of all UserAccess entities for a user where the user is admin of the Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetUsersUserAdminAccessList(string email);

        /// <summary>
        /// Gets a UserAccess entity with the specified AccessId.
        /// First checks the cache, if not found, gets the UserAccess from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess to get.</param>
        /// <returns>UserAccess object with the given AccessId. Null if the UserAccess doesn't exist.</returns>
        Task<UserAccess> GetUserAccess(int id);

        /// <summary>
        /// Adds a new UserAccess entity to the database and adds it to the cache.
        /// Updates the related lists of UserAccesses in the cache too.
        /// If the AccessLevel is 0 or the user's email address is in the Admins property of the Progeny, the user is added to the Admins list.
        /// </summary>
        /// <param name="userAccess">The UserAccess object to add.</param>
        /// <returns>The added UserAccess object.</returns>
        Task<UserAccess> AddUserAccess(UserAccess userAccess);

        /// <summary>
        /// Updates a UserAccess entity in the database and the cache.
        /// Updates the related lists of UserAccesses in the cache too.
        /// If the AccessLevel is 0 or the user's email address is in the Admins property of the Progeny, the user is added to the Admins list.
        /// </summary>
        /// <param name="userAccess">The UserAccess object with the updated properties.</param>
        /// <returns>The updated UserAccess object.</returns>
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);

        /// <summary>
        /// Removes a UserAccess entity from the database and the cache.
        /// Also updates the related lists of UserAccesses in the cache.
        /// Only admins of a Progeny can remove UserAccess entities that belong to the Progeny.
        /// If the UserAccess entity is an admin of the Progeny, the user is removed from the Admins list.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess entity to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the UserAccess belongs to.</param>
        /// <param name="userId">The email address of the UserId for the UserAccess to delete.</param>
        /// <returns></returns>
        Task RemoveUserAccess(int id, int progenyId, string userId);

        /// <summary>
        /// Gets the UserAccess entity for a specific User and a Progeny.
        /// First checks the cache, if not found, gets the UserAccess from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the UserAccess.</param>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserAccess with the given ProgenyId and email. Null if the UserAccess doesn't exist.</returns>
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
    }
}
