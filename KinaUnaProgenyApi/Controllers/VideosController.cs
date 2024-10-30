using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Videos.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="videosService"></param>
    /// <param name="commentsService"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="webNotificationsService"></param>
    /// <param name="timelineService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController(
        IAzureNotifications azureNotifications,
        IVideosService videosService,
        ICommentsService commentsService,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IWebNotificationsService webNotificationsService,
        ITimelineService timelineService)
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
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await videosService.GetVideosList(progenyId, accessLevelResult.Value);
                allItems = [.. allItems.Where(p => p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = await videosService.GetVideosList(progenyId, accessLevelResult.Value);
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

            List<Video> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

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
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> VideoViewModel(int id, [FromQuery] int sortBy = 1, [FromQuery] string tagFilter = "")
        {
            Video video = await videosService.GetVideo(id);
            if (video == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(video.ProgenyId, userEmail, video.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
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
                AccessLevel = video.AccessLevel,
                Author = video.Author
            };
            model.AccessLevelListEn[video.AccessLevel].Selected = true;
            model.AccessLevelListDa[video.AccessLevel].Selected = true;
            model.AccessLevelListDe[video.AccessLevel].Selected = true;
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
            List<Video> videosList = await videosService.GetVideosList(video.ProgenyId, accessLevelResult.Value);
            videosList = [.. videosList.OrderBy(p => p.VideoTime)];
            if (videosList.Count != 0)
            {
                if (!string.IsNullOrEmpty(tagFilter))
                {
                    videosList = [.. videosList.Where(p => p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
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
                model.VideoNumber = currentIndex + 1;
                model.VideoCount = videosList.Count;
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

                if (sortBy == 1)
                {
                    (model.NextVideo, model.PrevVideo) = (model.PrevVideo, model.NextVideo);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;

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
            Video video = await videosService.GetVideo(id);

            if (video == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(video.ProgenyId, userEmail, video.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

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
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Video> videosList = await videosService.GetVideosList(id, accessLevelResult.Value);
            
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
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Video> videosList = await videosService.GetVideosList(id, accessLevelResult.Value);

            if (videosList.Count == 0)
            {
                Video tempPicture = new();

                videosList.Add(tempPicture);
                return Ok(videosList);
            }

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
            Video video = await videosService.GetVideo(id);
            if (video == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(video.ProgenyId, userEmail, video.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

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
            Video video = await videosService.GetVideoByLink(videoLink, progenyId);
            if (video == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(video.ProgenyId, userEmail, video.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(video);

        }

        /// <summary>
        /// Adds a new Video to the database.
        /// Also adds a new CommentThread for the Video, a corresponding TimeLineItem and sends Notifications to users.
        /// </summary>
        /// <param name="model">The Video to add.</param>
        /// <returns>The added Video.</returns>
        // POST api/videos
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Video model)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(model.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }
            
            CommentThread commentThread = await commentsService.AddCommentThread();
            model.CommentThreadNumber = commentThread.Id;

            model = await videosService.AddVideo(model);
            await videosService.SetVideoInCache(model.VideoId);
            await commentsService.SetCommentsList(model.CommentThreadNumber);

            Progeny progeny = await progenyService.GetProgeny(model.ProgenyId);
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "New Video added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new video for " + progeny.NickName;

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVideoPropertiesForAdd(model);
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVideoNotification(model, userInfo, notificationTitle);
            return Ok(model);
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
            Video video = await videosService.GetVideo(id);

            // Todo: more validation of the values
            if (video == null)
            {
                return NotFound();
            }

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(value.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            video.Tags = value.Tags;
            video.AccessLevel = value.AccessLevel;
            video.Author = value.Author;
            video.VideoTime = value.VideoTime;
            video.Duration = value.Duration;
            video.Location = value.Location;
            video.Longtitude = value.Longtitude;
            video.Latitude = value.Latitude;
            video.Altitude = value.Altitude;

            video = await videosService.UpdateVideo(video);

            await videosService.SetVideoInCache(video.VideoId);
            await commentsService.SetCommentsList(video.CommentThreadNumber);

            
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVideoPropertiesForUpdate(video);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

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
            Video video = await videosService.GetVideo(id);
            if (video == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

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

            TimeLineItem existingTimeLineItem = await timelineService.GetTimeLineItemByItemId(video.VideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video);
            if (existingTimeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(existingTimeLineItem);
            }

            _ = await videosService.DeleteVideo(video);
            await videosService.RemoveVideoFromCache(video.VideoId, video.ProgenyId);

            Progeny progeny = await progenyService.GetProgeny(video.ProgenyId);
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "Video deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a video for " + progeny.NickName;
            TimeLineItem timeLineItem = new()
            {
                ProgenyId = video.ProgenyId,
                ItemId = video.VideoId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Video,
                AccessLevel = 0
            };

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVideoNotification(video, userInfo, notificationTitle);

            return NoContent();

        }
    }
}
