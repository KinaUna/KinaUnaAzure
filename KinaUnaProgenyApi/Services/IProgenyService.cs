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
        Task<Progeny> GetProgeny(int id, UserInfo currentUserInfo = null);

        /// <summary>
        /// Adds a new Progeny to the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to add.</param>
        /// <returns>The added Progeny.</returns>
        Task<Progeny> AddProgeny(Progeny progeny);

        /// <summary>
        /// Updates a Progeny in the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny with updated properties.</param>
        /// <returns>The updated Progeny.</returns>
        Task<Progeny> UpdateProgeny(Progeny progeny);

        /// <summary>
        /// Deletes a Progeny from the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to delete.</param>
        /// <returns>The deleted Progeny.</returns>
        Task<Progeny> DeleteProgeny(Progeny progeny);

        /// <summary>
        /// Gets a ProgenyInfo object by ProgenyId.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny the ProgenyInfo object belongs to.</param>
        /// <returns>The ProgenyInfo object for the given Progeny.</returns>
        Task<ProgenyInfo> GetProgenyInfo(int progenyId);

        /// <summary>
        /// Adds a new ProgenyInfo object to the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object to add to the database.</param>
        /// <returns>The added ProgenyInfo object.</returns>
        Task<ProgenyInfo> AddProgenyInfo(ProgenyInfo progenyInfo);

        /// <summary>
        /// Updates a ProgenyInfo entity in the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object with the updated properties.</param>
        /// <returns>The updated ProgenyInfo object.</returns>
        Task<ProgenyInfo> UpdateProgenyInfo(ProgenyInfo progenyInfo);

        /// <summary>
        /// Deletes a ProgenyInfo entity from the database.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object to remove.</param>
        /// <returns>The deleted ProgenyInfo object.</returns>
        Task<ProgenyInfo> DeleteProgenyInfo(ProgenyInfo progenyInfo);

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
