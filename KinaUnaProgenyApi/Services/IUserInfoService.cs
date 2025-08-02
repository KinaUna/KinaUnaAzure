using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserInfoService
    {
        /// <summary>
        /// Gets a list of all UserInfos in the database.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        Task<List<UserInfo>> GetAllUserInfos();

        /// <summary>
        /// Gets a UserInfo object by email address.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserInfo object with the given email address. Null if the UserInfo doesn't exist.</returns>
        Task<UserInfo> GetUserInfoByEmail(string userEmail);

        /// <summary>
        /// Gets a UserInfo entity from the database by email address and adds it to the cache.
        /// Also updates the cache for UserInfoById and UserInfoByUserId.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>The UserInfo with the given email address. Null if the UserInfo item doesn't exist.</returns>
        Task<UserInfo> SetUserInfoByEmail(string userEmail);

        /// <summary>
        /// Adds a new UserInfo to the database.
        /// </summary>
        /// <param name="userInfo">The UserInfo entity to add.</param>
        /// <returns>The added UserInfo object.</returns>
        Task<UserInfo> AddUserInfo(UserInfo userInfo);

        /// <summary>
        /// Updates a UserInfo entity in the database and the cache.
        /// </summary>
        /// <param name="userInfo">The UserInfo object with the updated properties.</param>
        /// <returns>The updated UserInfo object.</returns>
        Task<UserInfo> UpdateUserInfo(UserInfo userInfo);

        /// <summary>
        /// Deletes a UserInfo entity from the database and the cache.
        /// This is a hard delete, to soft delete a UserInfo entity, use the UpdateUserInfo method and set the Deleted property to true.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to delete.</param>
        /// <returns>The deleted UserInfo object.</returns>
        Task<UserInfo> DeleteUserInfo(UserInfo userInfo);

        /// <summary>
        /// Gets a UserInfo entity by Id.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The Id of the UserInfo item to get.</param>
        /// <returns>The UserInfo object with the given Id. Null if the UserInfo item doesn't exist.</returns>
        Task<UserInfo> GetUserInfoById(int id);

        /// <summary>
        /// Gets a UserInfo entity by UserId.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The UserId of the UserInfo entity to get.</param>
        /// <returns>The UserInfo object with the given UserId. Null if the UserInfo item doesn't exist.</returns>
        Task<UserInfo> GetUserInfoByUserId(string id);

        /// <summary>
        /// Gets a list of all UserInfos that have been marked as deleted.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        Task<List<UserInfo>> GetDeletedUserInfos();

        /// <summary>
        /// Updates the information of a deleted user.
        /// </summary>
        /// <remarks>This method updates the details of a user who has been marked as deleted. Ensure that
        /// the provided <paramref name="userInfo"/> object contains valid and complete information before calling this
        /// method.</remarks>
        /// <param name="userInfo">The <see cref="UserInfo"/> object containing the updated information for the deleted user. This parameter
        /// cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see
        /// cref="UserInfo"/> object.</returns>
        Task<UserInfo> UpdateDeletedUserInfo(UserInfo userInfo);

        /// <summary>
        /// Removes the specified user information from the collection of deleted user information.
        /// </summary>
        /// <remarks>This does not update the original userinfo entity. This method performs an asynchronous operation to remove the specified user
        /// information from the collection  of deleted user information. If the user information is not found, the
        /// method completes successfully and  returns <see langword="null"/>.</remarks>
        /// <param name="userInfo">The user information to be removed. This parameter cannot be <see langword="null"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the  <see
        /// cref="UserInfo"/> object that was removed, or <see langword="null"/> if the specified user information  was
        /// not found in the collection.</returns>
        Task<UserInfo> RemoveUserInfoFromDeletedUserInfos(UserInfo userInfo);

        /// <summary>
        /// Checks if the user is a KinaUna admin.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean, true if the user is a KinaUna admin.</returns>
        Task<bool> IsAdminUserId(string userId);

        /// <summary>
        /// Adds the specified user information to the collection of deleted user information.
        /// </summary>
        /// <param name="userInfo">The user information to add. This parameter cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the  <see cref="UserInfo"/>
        /// object that was added to the collection.</returns>
        Task<UserInfo> AddUserInfoToDeletedUserInfos(UserInfo userInfo);
    }
}
