using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Videos.
    /// </summary>
    /// <param name="videosService"></param>
    /// <param name="commentsService"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="webNotificationsService"></param>
    /// <param name="timelineService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController(
        IVideosService videosService,
        ICommentsService commentsService,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IWebNotificationsService webNotificationsService,
        ITimelineService timelineService,
        IAccessManagementService accessManagementService)
        : ControllerBase
    {
        /// <summary>
        /// Generates a VideoPageViewModel for a specific Progeny.
        /// </summary>
        /// <param name="pageSize">Number of Videos per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to get videos for.</param>
        /// <param name="tagFilter">Include only Videos with Tags containing the tagFilter string. If empty, include all videos.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <returns>VideoPageViewModel</returns>
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await videosService.GetVideosList(progenyId, currentUserInfo);
                allItems = [.. allItems.Where(p => p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = await videosService.GetVideosList(progenyId, currentUserInfo);
                allItems = [.. allItems.OrderBy(p => p.VideoTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
            List<string> tagsList = [];
            foreach (Video vid in allItems)
            {
                if (sortBy == 1)
                {
                    vid.VideoNumber = vidCount - videoCounter + 1;
                }
                else
                {
                    vid.VideoNumber = videoCounter;
                }

                videoCounter++;
                if (!string.IsNullOrEmpty(vid.Tags))
                {
                    List<string> pvmTags = [.. vid.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }

                if (vid.Duration == null) continue;

                vid.DurationHours = vid.Duration.Value.Hours.ToString();
                vid.DurationMinutes = vid.Duration.Value.Minutes.ToString();
                vid.DurationSeconds = vid.Duration.Value.Seconds.ToString();
                if (vid.DurationSeconds.Length == 1)
                {
                    vid.DurationSeconds = "0" + vid.DurationSeconds;
                }

                if (vid.Duration.Value.Hours == 0) continue;

                if (vid.DurationMinutes.Length == 1)
                {
                    vid.DurationMinutes = "0" + vid.DurationMinutes;
                }
            }

            List<Video> itemsOnPage = [.. allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)];

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await commentsService.GetCommentsList(vid.CommentThreadNumber);
            }

            VideoPageViewModel model = new()
            {
                VideosList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter
            };
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> VideoViewModel([FromBody] VideoViewModelRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(request.VideoId, currentUserInfo);
            
            if (video == null) return NotFound();

            if (request.Progenies == null || request.Progenies.Count == 0)
            {
                request.Progenies = [video.ProgenyId];
            }
            
            VideoViewModel model = new()
            {
                VideoId = video.VideoId,
                VideoType = video.VideoType,
                VideoTime = video.VideoTime,
                Duration = video.Duration,
                ProgenyId = video.ProgenyId,
                Owners = video.Owners,
                VideoLink = video.VideoLink,
                ThumbLink = video.ThumbLink,
                Author = video.Author,
                ItemPerMission = video.ItemPerMission
            };
            model.CommentThreadNumber = video.CommentThreadNumber;
            model.Tags = video.Tags;
            model.VideoNumber = 1;
            model.VideoCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(video.CommentThreadNumber);
            model.Location = video.Location;
            model.Longtitude = video.Longtitude;
            model.Latitude = video.Latitude;
            model.Altitude = video.Latitude;
            model.TagsList = "";
            List<string> tagsList = [];


            List<Video> videosList = [];
            foreach (int progenyId in request.Progenies)
            {
                List<Video> tempList = await videosService.GetVideosList(progenyId, currentUserInfo);

                if (progenyId == video.ProgenyId)
                {
                    tempList = [.. tempList.OrderBy(p => p.VideoTime)];
                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Video vid in tempList)
                    {
                        if (vid.VideoId == video.VideoId)
                        {
                            currentIndex = indexer;
                        }

                        indexer++;
                    }

                    model.VideoNumber = currentIndex + 1;

                    model.VideoCount = tempList.Count;
                }

                videosList.AddRange(tempList);
            }

            videosList = [.. videosList.OrderBy(p => p.VideoTime)];
            if (videosList.Count != 0)
            {
                if (!string.IsNullOrEmpty(request.TagFilter))
                {
                    videosList = [.. videosList.Where(p => p.Tags != null && p.Tags.Contains(request.TagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
                }

                int currentIndex = 0;
                int indexer = 0;
                foreach (Video vid in videosList)
                {
                    if (vid.VideoId == video.VideoId)
                    {
                        currentIndex = indexer;
                    }
                    indexer++;
                    if (string.IsNullOrEmpty(vid.Tags)) continue;

                    List<string> pvmTags = [.. vid.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
                
                if (currentIndex > 0)
                {
                    model.PrevVideo = videosList[currentIndex - 1].VideoId;
                }
                else
                {
                    model.PrevVideo = videosList.Last().VideoId;
                }

                if (currentIndex + 1 < videosList.Count)
                {
                    model.NextVideo = videosList[currentIndex + 1].VideoId;
                }
                else
                {
                    model.NextVideo = videosList.First().VideoId;
                }

                if (request.SortOrder == 1)
                {
                    (model.NextVideo, model.PrevVideo) = (model.PrevVideo, model.NextVideo);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagString in tagsList)
                {
                    tagItems = tagItems + "'" + tagString + "',";
                }

                tagItems = tagItems[..^1];
                tagItems += "]";
            }

            model.TagsList = tagItems;

            return Ok(model);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> TimelineVideoViewModel([FromBody] VideoViewModelRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(request.VideoId, currentUserInfo);

            if (video == null) return NotFound();

            if (request.Progenies == null || request.Progenies.Count == 0)
            {
                request.Progenies = [video.ProgenyId];
            }

            VideoViewModel model = new()
            {
                VideoId = video.VideoId,
                VideoType = video.VideoType,
                VideoTime = video.VideoTime,
                Duration = video.Duration,
                ProgenyId = video.ProgenyId,
                Owners = video.Owners,
                VideoLink = video.VideoLink,
                ThumbLink = video.ThumbLink,
                Author = video.Author
            };
            model.CommentThreadNumber = video.CommentThreadNumber;
            model.Tags = video.Tags;
            model.VideoNumber = 1;
            model.VideoCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(video.CommentThreadNumber);
            model.Location = video.Location;
            model.Longtitude = video.Longtitude;
            model.Latitude = video.Latitude;
            model.Altitude = video.Latitude;
            model.TagsList = "";
            
            return Ok(model);

        }

        /// <summary>
        /// Generates a VideoViewModel for a specific Video.
        /// </summary>
        /// <param name="id">The VideoId of the Video.</param>
        /// <returns>VideoViewModel</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> VideoElement(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(id, currentUserInfo);

            if (video == null) return NotFound();
            
            VideoViewModel model = new();
            model.SetVideoPropertiesFromVideoItem(video);
            model.VideoNumber = 0;
            model.VideoCount = 0;
            model.CommentsList = await commentsService.GetCommentsList(video.CommentThreadNumber);
            model.TagsList = "";

            return Ok(model);
        }

        /// <summary>
        /// Gets a list of all Videos for a specific Progeny that a user with a given access level can view.
        /// Includes Comments for each Video.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get videos for.</param>
        /// <returns>List of Videos.</returns>
        // GET api/videos/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Video> videosList = await videosService.GetVideosList(id, currentUserInfo);
            
            if (videosList.Count == 0) return Ok(videosList);

            foreach (Video video in videosList)
            {
                video.Comments = await commentsService.GetCommentsList(video.CommentThreadNumber);
            }

            return Ok(videosList);
        }

        /// <summary>
        /// Gets a list of all Videos for a specific Progeny that a user with a given access level can view.
        /// Does not include comments for each Video.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get videos for.</param>
        /// <returns>List of Videos.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> ProgenyVideosList(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Video> videosList = await videosService.GetVideosList(id, currentUserInfo);

            if (videosList.Count != 0) return Ok(videosList);
            Video tempVideo = new();

            videosList.Add(tempVideo);

            return Ok(videosList);
        }

        /// <summary>
        /// Gets the video with the Given VideoId.
        /// Does not include comments.
        /// </summary>
        /// <param name="id">The VideoId of the Video.</param>
        /// <returns>Video</returns>
        // GET api/videos/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(id, currentUserInfo);
            if (video == null) return NotFound();
            
            return Ok(video);

        }

        /// <summary>
        /// Gets the video with the given VideoLink.
        /// </summary>
        /// <param name="videoLink">String with the VideoLink of the video.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny.</param>
        /// <returns>Video.</returns>
        [HttpGet("[action]/{videoLink}/{progenyId:int}")]
        public async Task<IActionResult> ByLink(string videoLink, int progenyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideoByLink(videoLink, progenyId, currentUserInfo);
            if (video == null) return NotFound();
            
            return Ok(video);

        }

        /// <summary>
        /// Adds a new Video to the database.
        /// Also adds a new CommentThread for the Video, a corresponding TimeLineItem and sends Notifications to users.
        /// </summary>
        /// <param name="value">The Video to add.</param>
        /// <returns>The added Video.</returns>
        // POST api/videos
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Video value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            if (!await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return Unauthorized();
            }

            CommentThread commentThread = await commentsService.AddCommentThread();
            value.CommentThreadNumber = commentThread.Id;
            value.CreatedBy = User.GetUserId();
            value.CreatedTime = DateTime.UtcNow;
            value.ModifiedBy = User.GetUserId();
            value.ModifiedTime = DateTime.UtcNow;

            value = await videosService.AddVideo(value, currentUserInfo);
            await videosService.SetVideoInCache(value.VideoId);
            await commentsService.SetCommentsList(value.CommentThreadNumber);

            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId, currentUserInfo);
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "New Video added for " + progeny.NickName;
            
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVideoPropertiesForAdd(value);
            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            await webNotificationsService.SendVideoNotification(value, userInfo, notificationTitle);

            value = await videosService.GetVideo(value.VideoId, currentUserInfo);
            value.Comments = await commentsService.GetCommentsList(value.CommentThreadNumber);

            return Ok(value);
        }

        /// <summary>
        /// Updates a Video. Only users with the appropriate access level can update a Video.
        /// Also updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The VideoId of the Video to update.</param>
        /// <param name="value">Video object with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        // PUT api/videos/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Video value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(id, currentUserInfo);

            // Todo: more validation of the values
            if (video == null)
            {
                return NotFound();
            }
            
            video.Tags = value.Tags;
            video.Author = value.Author;
            video.VideoTime = value.VideoTime;
            video.Duration = value.Duration;
            video.Location = value.Location;
            video.Longtitude = value.Longtitude;
            video.Latitude = value.Latitude;
            video.Altitude = value.Altitude;
            video.ModifiedBy = User.GetUserId();
            video.ModifiedTime = DateTime.UtcNow;
            video.ItemPermissionsDtoList = value.ItemPermissionsDtoList;
            video = await videosService.UpdateVideo(video, currentUserInfo);
            if (video == null)
            {
                return Unauthorized();
            }

            await videosService.SetVideoInCache(video.VideoId);
            await commentsService.SetCommentsList(video.CommentThreadNumber);

            
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVideoPropertiesForUpdate(video);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);

            video = await videosService.GetVideo(video.VideoId, currentUserInfo);
            video.Comments = await commentsService.GetCommentsList(video.CommentThreadNumber);

            return Ok(video);
        }

        /// <summary>
        /// Deletes a Video. Only users with the appropriate access level can delete a Video.
        /// Also deletes the corresponding CommentThread and TimeLineItem.
        /// Sends Notifications to admins for the Progeny.
        /// </summary>
        /// <param name="id">The VideoId of the Video to delete. </param>
        /// <returns>NoContent.</returns>
        // DELETE api/progeny/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Video video = await videosService.GetVideo(id, currentUserInfo);
            if (video == null) return NotFound();
            
            video.ModifiedBy = User.GetUserId();
            video.ModifiedTime = DateTime.UtcNow;

            Video deletedVideo = await videosService.DeleteVideo(video, currentUserInfo);
            if (deletedVideo == null)
            {
                return Unauthorized();
            }

            await videosService.RemoveVideoFromCache(video.VideoId, video.ProgenyId);

            List<Comment> comments = await commentsService.GetCommentsList(video.CommentThreadNumber);
            if (comments.Count != 0)
            {
                foreach (Comment deletedComment in comments)
                {
                    _ = await commentsService.DeleteComment(deletedComment);
                }
            }

            CommentThread cmntThread = await commentsService.GetCommentThread(video.CommentThreadNumber);
            if (cmntThread != null)
            {
                _ = await commentsService.DeleteCommentThread(cmntThread);
                await commentsService.RemoveCommentsList(video.CommentThreadNumber);
            }

            TimeLineItem existingTimeLineItem = await timelineService.GetTimeLineItemByItemId(video.VideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video, currentUserInfo);
            if (existingTimeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(existingTimeLineItem, currentUserInfo);
            }

            Progeny progeny = await progenyService.GetProgeny(video.ProgenyId, currentUserInfo);
            string notificationTitle = "Video deleted for " + progeny.NickName;
            
            await webNotificationsService.SendVideoNotification(video, currentUserInfo, notificationTitle);

            return NoContent();

        }
    }
}
