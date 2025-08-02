using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the UserInfos API.
    /// </summary>
    public interface IUserInfosHttpClient
    {
        /// <summary>
        /// Gets a user's UserInfo from the email address.
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <returns>The UserInfo with the given email address. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        Task<UserInfo> GetUserInfo(string email);

        /// <summary>
        /// Gets a user's information from the UserId.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>The UserInfo with the given UserId. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        Task<UserInfo> GetUserInfoByUserId(string userId);

        /// <summary>
        /// Adds a new UserInfo object.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to add.</param>
        /// <returns>The added UserInfo object. If an error occurs a new UserInfo with Id=0 is returned.</returns>
        Task<UserInfo> AddUserInfo(UserInfo userInfo);

        /// <summary>
        /// Updates a UserInfo object. The UserInfo with the same Id will be updated.
        /// </summary>
        /// <param name="userInfo">The UserInfo object with the updated properties.</param>
        /// <returns>UserInfo: The updated UserInfo object. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        Task<UserInfo> UpdateUserInfo(UserInfo userInfo);

        /// <summary>
        /// Deletes a UserInfo object.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to delete.</param>
        /// <returns>The deleted UserInfo object. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        Task<UserInfo> DeleteUserInfo(UserInfo userInfo);

        /// <summary>
        /// Checks if the current user's account is active.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>If the user is still active the UserInfo object of the user. If inactive a new UserInfo object with Id=0.</returns>
        Task<UserInfo> CheckCurrentUser(string userId);

        /// <summary>
        /// Gets the list of all soft-deleted UserInfos.
        /// Only KinaUnaAdmins are allowed to get all deleted UserInfo entities.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        Task<List<UserInfo>> GetDeletedUserInfos();

        /// <summary>
        /// Permanently deletes a UserInfo entity.
        /// To soft-delete a UserInfo use update method and set the Deleted property to true.
        /// </summary>
        /// <param name="userInfo">The UserInfo entity to delete.</param>
        /// <returns>The deleted UserInfo object.</returns>
        Task<UserInfo> RemoveUserInfoForGood(UserInfo userInfo);

        /// <summary>
        /// Get a list of all UserInfos.
        /// Includes soft-deleted entities.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        Task GetAllUserInfos();
    }
}
