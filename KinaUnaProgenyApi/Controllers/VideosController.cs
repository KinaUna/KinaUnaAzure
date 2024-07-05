using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
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
        // GET api/videos/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await videosService.GetVideosList(progenyId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = await videosService.GetVideosList(progenyId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime)];
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
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> VideoViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1, [FromQuery] string tagFilter = "")
        {
            Video video = await videosService.GetVideo(id);
            if (video == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail);
            if (userAccess == null && video.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
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
            List<Video> videosList = await videosService.GetVideosList(video.ProgenyId);
            videosList = [.. videosList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime)];
            if (videosList.Count != 0)
            {
                if (!string.IsNullOrEmpty(tagFilter))
                {
                    videosList = [.. videosList.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
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

        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> VideoElement(int id)
        {
            Video video = await videosService.GetVideo(id);

            if (video == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail);

            if ((userAccess == null && video.ProgenyId != Constants.DefaultChildId) || (userAccess != null && userAccess.AccessLevel > video.AccessLevel && video.ProgenyId != Constants.DefaultChildId))
            {
                return Unauthorized();
            }

            VideoViewModel model = new();
            model.SetVideoPropertiesFromVideoItem(video);
            model.VideoNumber = 0;
            model.VideoCount = 0;
            model.CommentsList = await commentsService.GetCommentsList(video.CommentThreadNumber);
            model.TagsList = "";
            return Ok(model);

        }

        // GET api/videos/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> Progeny(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Video> videosList = await videosService.GetVideosList(id);
            videosList = videosList.Where(v => v.AccessLevel >= accessLevel).ToList();
            if (videosList.Count == 0) return Ok(videosList);

            foreach (Video video in videosList)
            {
                video.Comments = await commentsService.GetCommentsList(video.CommentThreadNumber);
            }
            return Ok(videosList);
        }

        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> ProgenyVideosList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Video> videosList = await videosService.GetVideosList(id);
            videosList = videosList.Where(p => p.AccessLevel >= accessLevel).ToList();

            if (videosList.Count != 0)
            {
                return Ok(videosList);
            }

            Video tempPicture = new();
            
            videosList.Add(tempPicture);
            return Ok(videosList);
        }

        // GET api/videos/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            Video result = await videosService.GetVideo(id);
            if (result == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            return Ok(result);

        }

        [HttpGet("[action]/{videoLink}/{progenyId:int}")]
        public async Task<IActionResult> ByLink(string videoLink, int progenyId)
        {
            Video result = await videosService.GetVideoByLink(videoLink, progenyId);
            if (result == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            return Ok(result);

        }

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

            Video vid = await videosService.GetVideoByLink(model.VideoLink, model.ProgenyId);
            if (vid == null)
            {
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

            model = vid;

            return Ok(model);
        }

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

            //Progeny progeny = await progenyService.GetProgeny(video.ProgenyId);
            //UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            //string notificationTitle = "Video Edited for " + progeny.NickName;
            //string notificationMessage = userInfo.FullName() + " edited a video for " + progeny.NickName;

            // await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            // await webNotificationsService.SendVideoNotification(video, userInfo, notificationTitle);
            return Ok(video);
        }

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

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVideoMobile(int id)
        {
            Video result = await videosService.GetVideo(id);

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = 2, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems = await videosService.GetVideosList(progenyId);
            List<string> tagsList = [];
            foreach (Video vid in allItems)
            {
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
            
            if (!string.IsNullOrEmpty(tagFilter))
            {

                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
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

            List<Video> itemsOnPage = allItems.Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();

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
    }
}
