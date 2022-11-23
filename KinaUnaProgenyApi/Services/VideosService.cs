using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class VideosService: IVideosService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public VideosService(MediaDbContext mediaContext, IDistributedCache cache)
        {
            _mediaContext = mediaContext;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }
        
        public async Task<Video> GetVideo(int id)
        {
            Video video = await GetVideoFromCache(id);
            if (video == null || video.VideoId == 0)
            {
                video = await SetVideoInCache(id);
            }
            
            return video;
        }

        public async Task<Video> GetVideoByLink(string link, int progenyId)
        {
            Video result = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoLink == link && v.ProgenyId == progenyId);
            return result;
        }

        private async Task<Video> GetVideoFromCache(int id)
        {
            Video video = new Video();
            string cachedVideo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id);
            if (!string.IsNullOrEmpty(cachedVideo))
            {
                video = JsonConvert.DeserializeObject<Video>(cachedVideo);
            }

            return video;
        }

        public async Task<Video> SetVideoInCache(int id)
        {
            Video video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            if (video != null)
            {
                await SetVideosListInCache(video.ProgenyId);
            }

            return video;
        }

        public async Task<Video> AddVideo(Video video)
        {
            _ = await _mediaContext.VideoDb.AddAsync(video);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetVideoInCache(video.VideoId);
            return video;
        }

        public async Task<Video> UpdateVideo(Video video)
        {
            Video videoToUpdate = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToUpdate != null)
            {
                videoToUpdate.Location = video.Location;
                videoToUpdate.AccessLevel = video.AccessLevel;
                videoToUpdate.ProgenyId = video.ProgenyId;
                videoToUpdate.CommentThreadNumber = video.CommentThreadNumber;
                videoToUpdate.Altitude = video.Altitude;
                videoToUpdate.Duration = video.Duration;
                videoToUpdate.DurationHours = video.DurationHours;
                videoToUpdate.DurationMinutes = video.DurationMinutes;
                videoToUpdate.DurationSeconds = video.DurationSeconds;
                videoToUpdate.Author = video.Author;
                videoToUpdate.Latitude = video.Latitude;
                videoToUpdate.Longtitude = video.Longtitude;
                videoToUpdate.Owners = video.Owners;
                videoToUpdate.Tags = video.Tags;
                videoToUpdate.VideoLink = video.VideoLink;
                videoToUpdate.VideoNumber = video.VideoNumber;
                videoToUpdate.VideoTime = video.VideoTime;
                videoToUpdate.ThumbLink = video.ThumbLink;
                videoToUpdate.TimeZone = video.TimeZone;
                videoToUpdate.VideoType = video.VideoType;
                videoToUpdate.Progeny = video.Progeny;
                videoToUpdate.Comments = video.Comments;

                _ = _mediaContext.VideoDb.Update(videoToUpdate);
                _ = await _mediaContext.SaveChangesAsync();

                _ = await SetVideoInCache(videoToUpdate.VideoId);
            }
            
            return video;
        }

        public async Task<Video> DeleteVideo(Video video)
        {
            Video videoToDelete = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToDelete != null)
            {
                _mediaContext.VideoDb.Remove(videoToDelete);
                _ = await _mediaContext.SaveChangesAsync();
                await RemoveVideoFromCache(videoToDelete.VideoId, videoToDelete.ProgenyId);
            }
            
            return video;
        }

        public async Task RemoveVideoFromCache(int videoId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "video" + videoId);
            _ = await SetVideosListInCache(progenyId);
        }

        public async Task<List<Video>> GetVideosList(int progenyId)
        {
            List<Video> videosList = await GetVideosListFromCache(progenyId);
            if (!videosList.Any())
            {
                videosList = await SetVideosListInCache(progenyId);
            }

            return videosList;
        }

        private async Task<List<Video>> GetVideosListFromCache(int progenyId)
        {
            List<Video> videosList = new List<Video>();
            string cachedVideosList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonConvert.DeserializeObject<List<Video>>(cachedVideosList);
            }

            return videosList;
        }

        public async Task<List<Video>> SetVideosListInCache(int progenyId)
        {
            List<Video> videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);

            return videosList;
        }
    }
}
