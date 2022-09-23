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
            Video video;
            string cachedVideo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id);
            if (!string.IsNullOrEmpty(cachedVideo))
            {
                video = JsonConvert.DeserializeObject<Video>(cachedVideo);
            }
            else
            {
                video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            }

            return video;
        }

        public async Task<Video> GetVideoByLink(string link, int progenyId)
        {
            Video result = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoLink == link && v.ProgenyId == progenyId);
            return result;
        }

        public async Task<Video> SetVideo(int id)
        {
            Video video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            if (video != null)
            {
                await SetVideosList(video.ProgenyId);
            }

            return video;
        }

        public async Task<Video> AddVideo(Video video)
        {
            await _mediaContext.VideoDb.AddAsync(video);
            await _mediaContext.SaveChangesAsync();
            return video;
        }

        public async Task<Video> UpdateVideo(Video video)
        {
            _mediaContext.VideoDb.Update(video);
            await _mediaContext.SaveChangesAsync();
            return video;
        }

        public async Task<Video> DeleteVideo(Video video)
        {
            _mediaContext.VideoDb.Remove(video);
            await _mediaContext.SaveChangesAsync();
            return video;
        }

        public async Task RemoveVideo(int videoId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "video" + videoId);

            await SetVideosList(progenyId);
        }

        public async Task<List<Video>> GetVideosList(int progenyId)
        {
            List<Video> videosList;
            string cachedVideosList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonConvert.DeserializeObject<List<Video>>(cachedVideosList);
            }
            else
            {
                videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);
            }

            return videosList;
        }

        public async Task<List<Video>> SetVideosList(int progenyId)
        {
            List<Video> videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);

            return videosList;
        }
    }
}
