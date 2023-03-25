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
    public class VideosController : ControllerBase
    {
        private readonly IVideosService _videosService;
        private readonly ICommentsService _commentsService;
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;
        private readonly ITimelineService _timelineService;

        public VideosController(IAzureNotifications azureNotifications, IVideosService videosService, ICommentsService commentsService, IProgenyService progenyService,
            IUserInfoService userInfoService, IUserAccessService userAccessService, IWebNotificationsService webNotificationsService, ITimelineService timelineService)
        {
            _azureNotifications = azureNotifications;
            _videosService = videosService;
            _commentsService = commentsService;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _webNotificationsService = webNotificationsService;
            _timelineService = timelineService;
        }

        // GET api/videos/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

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
                allItems = await _videosService.GetVideosList(progenyId);
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
            }
            else
            {
                allItems = await _videosService.GetVideosList(progenyId); 
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
            List<string> tagsList = new();
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
                if (!String.IsNullOrEmpty(vid.Tags))
                {
                    List<string> pvmTags = vid.Tags.Split(',').ToList();
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }

                if (vid.Duration != null)
                {
                    vid.DurationHours = vid.Duration.Value.Hours.ToString();
                    vid.DurationMinutes = vid.Duration.Value.Minutes.ToString();
                    vid.DurationSeconds = vid.Duration.Value.Seconds.ToString();
                    if (vid.DurationSeconds.Length == 1)
                    {
                        vid.DurationSeconds = "0" + vid.DurationSeconds;
                    }
                    if (vid.Duration.Value.Hours != 0)
                    {
                        if (vid.DurationMinutes.Length == 1)
                        {
                            vid.DurationMinutes = "0" + vid.DurationMinutes;
                        }

                    }
                }
            }
            
            List<Video> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await _commentsService.GetCommentsList(vid.CommentThreadNumber);
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
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> VideoViewModel(int id, int accessLevel, [FromQuery] int sortBy=1, [FromQuery] string tagFilter="")
        {
            Video video = await _videosService.GetVideo(id); 
            if (video != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail); 
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
                model.CommentsList = await _commentsService.GetCommentsList(video.CommentThreadNumber); 
                model.Location = video.Location;
                model.Longtitude = video.Longtitude;
                model.Latitude = video.Latitude;
                model.Altitude = video.Latitude;
                model.TagsList = "";
                List<string> tagsList = new();
                List<Video> videosList = await _videosService.GetVideosList(video.ProgenyId); 
                videosList = videosList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
                if (videosList.Any())
                {
                    if (!string.IsNullOrEmpty(tagFilter))
                    {
                        videosList = videosList.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
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
                        if (!string.IsNullOrEmpty(vid.Tags))
                        {
                            List<string> pvmTags = vid.Tags.Split(',').ToList();
                            foreach (string tagstring in pvmTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                    model.VideoNumber = currentIndex + 1;
                    model.VideoCount = videosList.Count;
                    if(currentIndex > 0)
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
                if (tagsList.Any())
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

            return NotFound();
        }

        // GET api/videos/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> Progeny(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Video> videosList = await _videosService.GetVideosList(id);
            videosList = videosList.Where(v => v.AccessLevel >= accessLevel).ToList();
            if (videosList.Any())
            {
                foreach (Video video in videosList)
                {
                    video.Comments = await _commentsService.GetCommentsList(video.CommentThreadNumber); 
                }
            }
            return Ok(videosList);
        }

        
        // GET api/videos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            Video result = await _videosService.GetVideo(id);
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                return Ok(result);
            }

            return NotFound();
        }

        [HttpGet("[action]/{videoLink}/{progenyId}")]
        public async Task<IActionResult> ByLink(string videoLink, int progenyId)
        {
            Video result = await _videosService.GetVideoByLink(videoLink, progenyId);
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                return Ok(result);
            }

            return NotFound();
        }

        // POST api/videos
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Video model)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(model.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            Video vid = await _videosService.GetVideoByLink(model.VideoLink, model.ProgenyId);
            if (vid == null)
            {
                CommentThread commentThread = await _commentsService.AddCommentThread();
                model.CommentThreadNumber = commentThread.Id;

                model = await _videosService.AddVideo(model);
                await _videosService.SetVideoInCache(model.VideoId);
                await _commentsService.SetCommentsList(model.CommentThreadNumber);

                Progeny progeny = await _progenyService.GetProgeny(model.ProgenyId);
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
                string notificationTitle = "New Video added for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " added a new video for " + progeny.NickName;
                
                TimeLineItem timeLineItem = new();
                timeLineItem.CopyVideoPropertiesForAdd(model);
                _ = await _timelineService.AddTimeLineItem(timeLineItem);
                
                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendVideoNotification(model, userInfo, notificationTitle);
                return Ok(model);
            }

            model = vid;

            return Ok(model);
        }

        // PUT api/videos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Video value)
        {
            Video video = await _videosService.GetVideo(id);

            // Todo: more validation of the values
            if (video == null)
            {
                return NotFound();
            }

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(value.ProgenyId, userEmail);

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

            video = await _videosService.UpdateVideo(video);
            
            await _videosService.SetVideoInCache(video.VideoId);
            await _commentsService.SetCommentsList(video.CommentThreadNumber);

            Progeny progeny = await _progenyService.GetProgeny(video.ProgenyId);
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "Video Edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a video for " + progeny.NickName;
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVideoPropertiesForUpdate(video);
            _ = await _timelineService.UpdateTimeLineItem(timeLineItem);

            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendVideoNotification(video, userInfo, notificationTitle);
            return Ok(video);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Video video = await _videosService.GetVideo(id);
            if (video != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail);

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                List<Comment> comments = await _commentsService.GetCommentsList(video.CommentThreadNumber);
                if (comments.Any())
                {
                    foreach (Comment deletedComment in comments)
                    {
                        _ = await _commentsService.DeleteComment(deletedComment);
                        await _commentsService.RemoveComment(deletedComment.CommentId, deletedComment.CommentThreadNumber);
                    }
                }

                CommentThread cmntThread = await _commentsService.GetCommentThread(video.CommentThreadNumber);
                if (cmntThread != null)
                {
                    _ = await _commentsService.DeleteCommentThread(cmntThread);
                    await _commentsService.RemoveCommentsList(video.CommentThreadNumber);
                }

                TimeLineItem existingTimeLineItem = await _timelineService.GetTimeLineItemByItemId(video.VideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video);
                if (existingTimeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(existingTimeLineItem);
                }

                _ = await _videosService.DeleteVideo(video);
                await _videosService.RemoveVideoFromCache(video.VideoId, video.ProgenyId);

                Progeny progeny = await _progenyService.GetProgeny(video.ProgenyId);
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
                string notificationTitle = "Video deleted for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " deleted a video for " + progeny.NickName;
                TimeLineItem timeLineItem = new()
                {
                    ProgenyId = video.ProgenyId,
                    ItemId = video.VideoId.ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.Video,
                    AccessLevel = 0
                };

                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendVideoNotification(video, userInfo, notificationTitle);

                return NoContent();
            }
            else
            {
                return NotFound();
            }

        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVideoMobile(int id)
        {
            Video result = await _videosService.GetVideo(id);

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = 2, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            allItems = await _videosService.GetVideosList(progenyId);
            List<string> tagsList = new();
            foreach (Video vid in allItems)
            {
                if (!String.IsNullOrEmpty(vid.Tags))
                {
                    List<string> pvmTags = vid.Tags.Split(',').ToList();
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(tagFilter))
            {

                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
            }
            else
            {
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
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
                
                if (vid.Duration != null)
                {
                    vid.DurationHours = vid.Duration.Value.Hours.ToString();
                    vid.DurationMinutes = vid.Duration.Value.Minutes.ToString();
                    vid.DurationSeconds = vid.Duration.Value.Seconds.ToString();
                    if (vid.DurationSeconds.Length == 1)
                    {
                        vid.DurationSeconds = "0" + vid.DurationSeconds;
                    }
                    if (vid.Duration.Value.Hours != 0)
                    {
                        if (vid.DurationMinutes.Length == 1)
                        {
                            vid.DurationMinutes = "0" + vid.DurationMinutes;
                        }

                    }
                }
            }

            List<Video> itemsOnPage = allItems.Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await _commentsService.GetCommentsList(vid.CommentThreadNumber);
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
