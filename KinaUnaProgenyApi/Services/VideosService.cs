using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class VideosService : IVideosService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public VideosService(MediaDbContext mediaContext, IDistributedCache cache, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _mediaContext = mediaContext;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }

        /// <summary>
        /// Gets a Video entity with the specified VideoId.
        /// First checks the cache, if not found, gets the Video from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The VideoId of the Video to get.</param>
        /// <param name="currentUserInfo">The UserInfo object of the current user.For checking permissions.</param>
        /// <returns>The Video object with the given VideoId. Null if the Video item doesn't exist.</returns>
        public async Task<Video> GetVideo(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Video video = await GetVideoFromCache(id);
            if (video == null || video.VideoId == 0)
            {
                video = await SetVideoInCache(id);
            }

            if (video != null && video.VideoId != 0)
            {
                video.ItemPermission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, video.VideoId, video.ProgenyId, 0, currentUserInfo);
            }

            return video;
        }

        /// <summary>
        /// Gets a Video entity with the specified VideoLink.
        /// </summary>
        /// <param name="link">The VideoLink of the Video item to get.</param>
        /// <param name="progenyId">The ProgenyId of the Video item to get.</param>
        /// <param name="currentUserInfo">The UserInfo object of the current user.For checking permissions.</param>
        /// <returns>The Video with the given VideoLink and ProgenyId. Null if the Video doesn't exist.</returns>
        public async Task<Video> GetVideoByLink(string link, int progenyId, UserInfo currentUserInfo)
        {
            Video result = await _mediaContext.VideoDb.FirstOrDefaultAsync(v => v.VideoLink == link && v.ProgenyId == progenyId);
            if (result != null)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, result.VideoId, currentUserInfo, PermissionLevel.View))
                {
                    result.ItemPermission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, result.VideoId, result.ProgenyId, 0, currentUserInfo);
                    return result;
                }
            }
            return null;
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

            Video video = JsonSerializer.Deserialize<Video>(cachedVideo, JsonSerializerOptions.Web);
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
            if (video.Tags != null && video.Tags.Trim().EndsWith(','))
            {
                video.Tags = video.Tags.Trim().TrimEnd(',');
                video = await UpdateVideoAsSystem(video);
            }

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "video" + id, JsonSerializer.Serialize(video, JsonSerializerOptions.Web), _cacheOptionsSliding);
            
            _ = await SetVideosListInCache(video.ProgenyId);

            return video;
        }

        /// <summary>
        /// Adds a new Video entity to the database and adds it to the cache.
        /// </summary>
        /// <param name="video">The Video object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Video object.</returns>
        public async Task<Video> AddVideo(Video video, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(video.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            video.RemoveNullStrings();

            _ = _mediaContext.VideoDb.Add(video);
            _ = await _mediaContext.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Video, video.VideoId, video.ProgenyId, 0, video.ItemPermissionsDtoList, currentUserInfo);

            _ = await SetVideoInCache(video.VideoId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(video.ProgenyId, 0, KinaUnaTypes.TimeLineType.Video);

            return video;
        }

        /// <summary>
        /// Updates a Video entity in the database and the cache.
        /// </summary>
        /// <param name="video">The Video object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Video object.</returns>
        public async Task<Video> UpdateVideo(Video video, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, video.VideoId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            video.RemoveNullStrings();

            Video videoToUpdate = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToUpdate == null) return null;

            videoToUpdate.CopyPropertiesForUpdate(video);

            _ = _mediaContext.VideoDb.Update(videoToUpdate);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Video, videoToUpdate.VideoId, videoToUpdate.ProgenyId, 0, videoToUpdate.ItemPermissionsDtoList, currentUserInfo);

            _ = await SetVideoInCache(videoToUpdate.VideoId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(videoToUpdate.ProgenyId, 0, KinaUnaTypes.TimeLineType.Video);

            return video;
        }

        /// <summary>
        /// Updates a Video entity in the database and the cache.
        /// End users should not use this method, only system processes.
        /// </summary>
        /// <param name="video">The Video object with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        private async Task<Video> UpdateVideoAsSystem(Video video)
        {
            video.RemoveNullStrings();

            Video videoToUpdate = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToUpdate == null) return null;
            string modifiedBy = videoToUpdate.ModifiedBy;
            DateTime modifiedTime = videoToUpdate.ModifiedTime;
            videoToUpdate.CopyPropertiesForUpdate(video);
            videoToUpdate.ModifiedBy = modifiedBy;
            videoToUpdate.ModifiedTime = modifiedTime;

            _ = _mediaContext.VideoDb.Update(videoToUpdate);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetVideoInCache(videoToUpdate.VideoId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(videoToUpdate.ProgenyId, 0, KinaUnaTypes.TimeLineType.Video);
            return video;
        }

        /// <summary>
        /// Deletes a Video entity from the database and the cache.
        /// </summary>
        /// <param name="video">The Video object to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Video object.</returns>
        public async Task<Video> DeleteVideo(Video video, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, video.VideoId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Video videoToDelete = await _mediaContext.VideoDb.SingleOrDefaultAsync(v => v.VideoId == video.VideoId);
            if (videoToDelete == null) return null;

            _mediaContext.VideoDb.Remove(videoToDelete);
            _ = await _mediaContext.SaveChangesAsync();

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, videoToDelete.VideoId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await RemoveVideoFromCache(videoToDelete.VideoId, videoToDelete.ProgenyId);

            await _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(videoToDelete.ProgenyId, 0, KinaUnaTypes.TimeLineType.Video);
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
        /// <param name="currentUserInfo">The UserInfo object of the current user.For checking permissions.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<List<Video>> GetVideosList(int progenyId, UserInfo currentUserInfo)
        {
            VideosListCacheEntry cacheEntry = await _kinaUnaCacheService.GetVideosListCache(currentUserInfo.UserId, progenyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = await _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Video);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.VideosList.ToList();
                }
            }

            Video[] videosList = await GetVideosListFromCache(progenyId);
            if (videosList.Length == 0)
            {
                videosList = await SetVideosListInCache(progenyId);
            }

            List<Video> filteredList = [];
            foreach (Video video in videosList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, video.VideoId, currentUserInfo, PermissionLevel.View))
                {
                    filteredList.Add(video);
                }
            }
            filteredList = filteredList.OrderByDescending(v => v.VideoTime).ToList();

            await _kinaUnaCacheService.SetVideoListCache(currentUserInfo.UserId, progenyId, filteredList.ToArray());

            return filteredList;
        }

        /// <summary>
        /// Retrieves a list of videos associated with the specified progeny that match the given tag.
        /// </summary>
        /// <remarks>The method filters videos by the specified tag in a case-insensitive manner. If no
        /// tag is provided, all videos for the specified progeny are returned.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose videos are to be retrieved.</param>
        /// <param name="tag">The tag to filter videos by. If null or empty, all videos for the specified progeny are returned.</param>
        /// <param name="currentUserInfo">The user information of the caller, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Video"/>
        /// objects that match the specified criteria.</returns>
        public async Task<List<Video>> GetVideosWithTag(int progenyId, string tag, UserInfo currentUserInfo)
        {
            List<Video> allItems = await GetVideosList(progenyId, currentUserInfo);
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
        private async Task<Video[]> GetVideosListFromCache(int progenyId)
        {
            Video[] videosList = [];
            string cachedVideosList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonSerializer.Deserialize<Video[]>(cachedVideosList, JsonSerializerOptions.Web);
            }

            return videosList;
        }

        /// <summary>
        /// Gets a list of all Videos for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set Videos for.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<Video[]> SetVideosListInCache(int progenyId)
        {
            Video[] videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "videoslist" + progenyId, JsonSerializer.Serialize(videosList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return videosList;
        }
    }
}
