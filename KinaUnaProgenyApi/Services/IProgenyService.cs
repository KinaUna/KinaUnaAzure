using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IProgenyService
    {
        /// <summary>
        /// Gets a Progeny by Id.
        /// First tries to get the Progeny from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get.</param>
        /// <returns>The Progeny with the given Id. Null if the Progeny doesn't exist.</returns>
        Task<Progeny> GetProgeny(int id);

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
        /// Resizes a Progeny profile picture and saves it to the image store.
        /// </summary>
        /// <param name="imageId">The current file name.</param>
        /// <returns>The new file name of the resized image.</returns>
        Task<string> ResizeImage(string imageId);
    }
}
