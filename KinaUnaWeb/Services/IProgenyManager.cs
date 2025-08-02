using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing user data.
    /// </summary>
    public interface IProgenyManager
    {
        /// <summary>
        /// Gets the UserInfo for a user with a given email.
        /// If the UserInfo is not found, a new UserInfo object is created and added to the database.
        /// If there is an error getting the UserInfo, a UserInfo object with UserId = "401" and UserName = the error message is returned, the user should be logged out in this case.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserInfo object.</returns>
        Task<UserInfo> GetInfo(string userEmail);

        /// <summary>
        /// Checks if the user's ApplicationUser data in the IDP database is valid.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean: True if the user data is valid.</returns>
        Task<bool> IsApplicationUserValid(string userId);

        /// <summary>
        /// Checks if the user's UserInfo object is valid.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean: True if the user data is valid.</returns>
        Task<bool> IsUserLoginValid(string userId);
    }
}
