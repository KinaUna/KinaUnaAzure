using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class VideosService : IVideosService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public VideosService(MediaDbContext mediaContext, IDistributedCache cache)
        {
            _mediaContext = mediaContext;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }

        /// <summary>
        /// Gets a Video entity with the specified VideoId.
        /// First checks the cache, if not found, gets the Video from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get.</param>
        /// <returns>The Video object with the given VideoId. Null if the Video item doesn't exist.</returns>
        public async Task<Video> GetVideo(int id)
        {
            Video video = await GetVideoFromCache(id);
            if (video == null || video.VideoId == 0)
            {
                video = await SetVideoInCache(id);
            }

            return video;
        }

        /// <summary>
        /// Gets a Video entity with the specified VideoLink.
        /// </summary>
        /// <param name="link">The VideoLink of the Video item to get.</param>
        /// <param name="progenyId">The ProgenyId of the Video item to get.</param>
        /// <returns>The Video with the given VideoLink and ProgenyId. Null if the Video doesn't exist.</returns>
        public async Task<Video> GetVideoByLink(string link, int progenyId)
        {
            Video result = await _mediaContext.VideoDb.FirstOrDefaultAsync(v => v.VideoLink == link && v.ProgenyId == progenyId);
            return result;
        }

        /// <summary>
        /// Gets the Video entity with the specified VideoId from the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get.</param>
        /// <returns>The Video with the given VideoId. Null if the Video isn't in the cache.</returns>
        private async Task<Video> GetVideoFromCache(int id)
        {
            string cachedVideo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id);
            if (string.IsNullOrEmpty(cachedVideo))
            {
                return null;
            }

            Video video = JsonConvert.DeserializeObject<Video>(cachedVideo);
            return video;
        }

        /// <summary>
        /// Gets a Video entity with the specified VideoId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get and set.</param>
        /// <returns>The Video object with the given VideoId. Null if the Video doesn't exist.</returns>
        public async Task<Video> SetVideoInCache(int id)
        {
            Video video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);

            if (video == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);

            if (video.Tags != null && video.Tags.Trim().EndsWith(','))
            {
                video.Tags = video.Tags.Trim().TrimEnd(',');
                _ = await UpdateVideo(video);
            }

            await SetVideosListInCache(video.ProgenyId);

            return video;
        }

        /// <summary>
        /// Adds a new Video entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="video">The Video object to add.</param>
        /// <returns>The added Video object.</returns>
        public async Task<Video> AddVideo(Video video)
        {
            video.RemoveNullStrings();

            _ = _mediaContext.VideoDb.Add(video);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetVideoInCache(video.VideoId);
            return video;
        }

        /// <summary>
        /// Updates a Video entity in the database and the cache.
        /// </summary>
        /// <param name="video">The Video object with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        public async Task<Video> UpdateVideo(Video video)
        {
            video.RemoveNullStrings();

            Video videoToUpdate = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToUpdate == null) return null;

            videoToUpdate.CopyPropertiesForUpdate(video);

            _ = _mediaContext.VideoDb.Update(videoToUpdate);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetVideoInCache(videoToUpdate.VideoId);

            return video;
        }

        /// <summary>
        /// Deletes a Video entity from the database and the cache.
        /// </summary>
        /// <param name="video">The Video object to delete.</param>
        /// <returns>The deleted Video object.</returns>
        public async Task<Video> DeleteVideo(Video video)
        {
            Video videoToDelete = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToDelete == null) return null;

            _mediaContext.VideoDb.Remove(videoToDelete);
            _ = await _mediaContext.SaveChangesAsync();
            await RemoveVideoFromCache(videoToDelete.VideoId, videoToDelete.ProgenyId);

            return video;
        }

        /// <summary>
        /// Removes a Video entity from the cache.
        /// Also updates the list of all Videos for the Progeny in the cache.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video item to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Video belongs to.</param>
        /// <returns></returns>
        public async Task RemoveVideoFromCache(int videoId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "video" + videoId);
            _ = await SetVideosListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Videos for a Progeny.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Videos for.</param>
        /// <param name="accessLevel">The access level of the user.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<List<Video>> GetVideosList(int progenyId, int accessLevel)
        {
            List<Video> videosList = await GetVideosListFromCache(progenyId);
            if (videosList.Count == 0)
            {
                videosList = await SetVideosListInCache(progenyId);
            }

            videosList = [.. videosList.Where(p => p.AccessLevel >= accessLevel)];

            return videosList;
        }

        public async Task<List<Video>> GetVideosWithTag(int progenyId, string tag, int accessLevel)
        {
            List<Video> allItems = await GetVideosList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(v => v.Tags != null && v.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }

        /// <summary>
        /// Gets a list of all Videos for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Videos for.</param>
        /// <returns>List of Video objects.</returns>
        private async Task<List<Video>> GetVideosListFromCache(int progenyId)
        {
            List<Video> videosList = [];
            string cachedVideosList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonConvert.DeserializeObject<List<Video>>(cachedVideosList);
            }

            return videosList;
        }

        /// <summary>
        /// Gets a list of all Videos for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set Videos for.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<List<Video>> SetVideosListInCache(int progenyId)
        {
            List<Video> videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);

            return videosList;
        }
    }
}
