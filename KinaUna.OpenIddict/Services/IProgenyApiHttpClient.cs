using KinaUna.Data.Models;

namespace KinaUna.OpenIddict.Services
{
    public interface IProgenyApiHttpClient
    {
        /// <summary>
        /// Retrieves user information for the specified user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose information is to be retrieved. Cannot be null or empty.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a <see
        /// cref="UserInfo"/> object with the user's details, or <see langword="null"/> if the user is not found.</returns>
        Task<UserInfo> GetUserInfoByUserId(string userId);

        /// <summary>
        /// Updates access lists to replace an old email address with a new one for a specified user.
        /// </summary>
        /// <remarks>This method ensures that all access lists associated with the specified user are
        /// updated to reflect the new email address. It is the caller's responsibility to ensure that the new email
        /// address is valid and unique within the system.</remarks>
        /// <param name="userId">The unique identifier of the user whose access lists are being updated.</param>
        /// <param name="oldEmail">The user's current email address to be replaced. Must not be null or empty.</param>
        /// <param name="newEmail">The new email address to associate with the user. Must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the update
        /// was successful; otherwise, <see langword="false"/>.</returns>
        Task<bool> UpdateAccessListsWithNewUserEmail(string userId, string oldEmail, string newEmail);

        /// <summary>
        /// Retrieves a list of user information for users who have been marked as deleted.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to fetch user information for deleted
        /// users.  The caller should await the returned task to ensure the operation completes before accessing the
        /// result.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="UserInfo"/> objects representing the deleted users. If no users are marked as deleted,  the list will
        /// be empty.</returns>
        Task<List<UserInfo>> GetDeletedUserInfos();

        /// <summary>
        /// Adds the specified user information to the collection of deleted user records.
        /// </summary>
        /// <param name="userInfo">The user information to add. Cannot be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added  <see
        /// cref="UserInfo"/> object if the operation is successful; otherwise, <see langword="null"/>.</returns>
        Task<UserInfo?> AddUserInfoToDeletedUserInfos(UserInfo userInfo);

        /// <summary>
        /// Updates the information of a deleted user in the system.
        /// </summary>
        /// <remarks>This method is intended for updating user information after a user has been marked as
        /// deleted.  Ensure that the provided <paramref name="userInfo"/> object contains valid and complete data
        /// before calling this method.</remarks>
        /// <param name="userInfo">The <see cref="UserInfo"/> object containing the updated information for the deleted user. This parameter
        /// cannot be null.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the updated <see
        /// cref="UserInfo"/> object.</returns>
        Task<UserInfo> UpdateDeletedUserInfo(UserInfo userInfo);

        /// <summary>
        /// Removes the specified user information from the collection of deleted user information.
        /// </summary>
        /// <param name="userInfo">The user information to be removed. Cannot be <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the removed  <see
        /// cref="UserInfo"/> object if it was successfully removed; otherwise, <see langword="null"/>.</returns>
        Task<UserInfo?> RemoveUserInfoFromDeletedUserInfos(UserInfo userInfo);
    }
}
