using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Dependency injection interface, for service that provides methods for interacting with the IDP/Authentication Server.
    /// </summary>
    public interface IAuthHttpClient
    {
        /// <summary>
        /// Checks if a UserInfo has been soft-deleted.
        /// Soft-deleted users are stored in the DeletedUsers table in the database.
        /// Soft-deleted users should be permanently deleted from the IDP accounts database after a certain time period.
        /// </summary>
        /// <param name="userInfo">The UserInfo for the user.</param>
        /// <returns>If a deleted UserInfo is found it is returned, else a new UserInfo with an empty string for UserId is returned.</returns>
        Task<UserInfo> CheckDeleteUser(UserInfo userInfo);

        /// <summary>
        /// Removes a soft-deleted UserInfo from the DeletedUsers table in the database.
        /// This should be called when a user has soft-deleted their account and wants to restore it.
        /// </summary>
        /// <param name="userInfo">The UserInfo of the user.</param>
        /// <returns>The UserInfo that was soft-deleted and needs to be restored. If it doesn't exist in the DeletedUsers table a new UserInfo with an empty string for UserId is returned.</returns>
        Task<UserInfo> RemoveDeleteUser(UserInfo userInfo);

        /// <summary>
        /// Checks if a user is a valid ApplicationUser in the IDP database.
        /// This is to prevent access if a user's UserInfo hasn't been deleted but the ApplicationUser has been deleted.
        /// </summary>
        /// <param name="userId">The UserId of the user.</param>
        /// <returns>True if the user exists, false if the user doesn't exist.</returns>
        Task<bool> IsApplicationUserValid(string userId);
    }
}
