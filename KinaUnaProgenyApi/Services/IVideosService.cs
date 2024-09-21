using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IVideosService
    {
        /// <summary>
        /// Gets a Video entity with the specified VideoId.
        /// First checks the cache, if not found, gets the Video from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get.</param>
        /// <returns>The Video object with the given VideoId. Null if the Video item doesn't exist.</returns>
        Task<Video> GetVideo(int id);

        /// <summary>
        /// Gets a Video entity with the specified VideoLink.
        /// </summary>
        /// <param name="link">The VideoLink of the Video item to get.</param>
        /// <param name="progenyId">The ProgenyId of the Video item to get.</param>
        /// <returns>The Video with the given VideoLink and ProgenyId. Null if the Video doesn't exist.</returns>
        Task<Video> GetVideoByLink(string link, int progenyId);

        /// <summary>
        /// Gets a Video entity with the specified VideoId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get and set.</param>
        /// <returns>The Video object with the given VideoId. Null if the Video doesn't exist.</returns>
        Task<Video> SetVideoInCache(int id);

        /// <summary>
        /// Adds a new Video entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="video">The Video object to add.</param>
        /// <returns>The added Video object.</returns>
        Task<Video> AddVideo(Video video);

        /// <summary>
        /// Updates a Video entity in the database and the cache.
        /// </summary>
        /// <param name="video">The Video object with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        Task<Video> UpdateVideo(Video video);

        /// <summary>
        /// Deletes a Video entity from the database and the cache.
        /// </summary>
        /// <param name="video">The Video object to delete.</param>
        /// <returns>The deleted Video object.</returns>
        Task<Video> DeleteVideo(Video video);

        /// <summary>
        /// Removes a Video entity from the cache.
        /// Also updates the list of all Videos for the Progeny in the cache.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video item to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Video belongs to.</param>
        /// <returns></returns>
        Task RemoveVideoFromCache(int videoId, int progenyId);

        /// <summary>
        /// Gets a list of all Videos for a Progeny.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Videos for.</param>
        /// <returns>List of Video objects.</returns>
        Task<List<Video>> GetVideosList(int progenyId);

        Task<List<Video>> GetVideosWithTag(int progenyId, string tag);

        /// <summary>
        /// Gets a list of all Videos for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set Videos for.</param>
        /// <returns>List of Video objects.</returns>
        Task<List<Video>> SetVideosListInCache(int progenyId);
    }
}
