using KinaUna.Data.Models;
using System.Collections.Generic;
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
        /// Retrieves a list of all progeny records.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to fetch all progeny data. The caller
        /// should await the returned task to ensure the operation completes before accessing the result.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Progeny"/>
        /// objects representing all progeny records. If no records are found, the list will be empty.</returns>
        Task<List<Progeny>> GetAllProgenies();
    }
}
