using KinaUna.Data.Models;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public interface IProgenyService
    {
        /// <summary>
        /// Gets a Progeny by Id.
        /// First tries to get the Progeny from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get.</param>
        /// <param name="currentUserInfo">Optional UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Progeny with the given Id. Null if the Progeny doesn't exist.</returns>
        Task<Progeny> GetProgeny(int id, UserInfo currentUserInfo);
        
        /// <summary>
        /// Adds a new Progeny to the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Progeny.</returns>
        Task<Progeny> AddProgeny(Progeny progeny, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Progeny in the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny with updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Progeny.</returns>
        Task<Progeny> UpdateProgeny(Progeny progeny, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Progeny from the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Progeny.</returns>
        Task<Progeny> DeleteProgeny(Progeny progeny, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a ProgenyInfo object by ProgenyId.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny the ProgenyInfo object belongs to.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The ProgenyInfo object for the given Progeny.</returns>
        Task<ProgenyInfo> GetProgenyInfo(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new ProgenyInfo object to the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object to add to the database.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added ProgenyInfo object.</returns>
        Task<ProgenyInfo> AddProgenyInfo(ProgenyInfo progenyInfo, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a ProgenyInfo entity in the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object with the updated properties.</param>
        /// <param name="currentUser"></param>
        /// <returns>The updated ProgenyInfo object.</returns>
        Task<ProgenyInfo> UpdateProgenyInfo(ProgenyInfo progenyInfo, UserInfo currentUser);

        /// <summary>
        /// Deletes a ProgenyInfo entity from the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object to remove.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The deleted ProgenyInfo object.</returns>
        Task<ProgenyInfo> DeleteProgenyInfo(ProgenyInfo progenyInfo, UserInfo currentUserInfo);

        /// <summary>
        /// Resizes a Progeny profile picture and saves it to the image store.
        /// </summary>
        /// <param name="imageId">The current file name.</param>
        /// <returns>The new file name of the resized image.</returns>
        Task<string> ResizeImage(string imageId);
        
        /// <summary>
        /// Updates the email address associated with a user in all relevant progeny records.
        /// </summary>
        /// <remarks>This method updates the user's email address in two contexts: <list type="bullet">
        /// <item><description>Progeny records where the user is listed as the progeny (primary
        /// email).</description></item> <item><description>Progeny records where the user is listed as an
        /// administrator.</description></item> </list> For progeny records where the user is an administrator, the old
        /// email address is removed from the admin list, and the new email address is added. Changes are persisted to
        /// the database after all updates are completed.</remarks>
        /// <param name="userInfo">The user information containing the current email address of the user.</param>
        /// <param name="newEmail">The new email address to associate with the user.</param>
        /// <returns></returns>
        Task ChangeUsersEmailForProgenies(UserInfo userInfo, string newEmail);

        /// <summary>
        /// Updates the progeny records associated with a new user based on their email address.
        /// </summary>
        /// <remarks>This method checks for any progeny records in the database that are associated with
        /// the specified user's email address. If such records are found, their <see cref="Progeny.UserId"/> is updated
        /// to match the user's unique identifier.</remarks>
        /// <param name="userInfo">The user information containing the user's email address and unique identifier.</param>
        /// <returns></returns>
        Task UpdateProgeniesForNewUser(UserInfo userInfo);
    }
}
