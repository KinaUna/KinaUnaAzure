using KinaUnaMediaApi.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaMediaApi.Services;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        private readonly MediaDbContext _context;
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly IDataService _dataService;

        public VideosController(MediaDbContext context, ProgenyDbContext progenyDbContext, IDataService dataService)
        {
            _context = context;
            _progenyDbContext = progenyDbContext;
            _dataService = dataService;
        }
        
        // GET api/videos/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems; 
            if (tagFilter != "")
            {
                allItems = await _dataService.GetVideosList(progenyId); // await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
            }
            else
            {
                allItems = await _dataService.GetVideosList(progenyId); // await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
            List<string> tagsList = new List<string>();
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
            
            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await _dataService.GetCommentsList(vid.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == vid.CommentThreadNumber).ToListAsync();
            }
            VideoPageViewModel model = new VideoPageViewModel();
            model.VideosList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
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
        public async Task<IActionResult> VideoViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {
            Video video = await _dataService.GetVideo(id); // await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            if (video != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(video.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == video.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null && video.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                VideoViewModel model = new VideoViewModel();
                model.VideoId = video.VideoId;
                model.VideoType = video.VideoType;
                model.VideoTime = video.VideoTime;
                model.Duration = video.Duration;
                model.ProgenyId = video.ProgenyId;
                model.Owners = video.Owners;
                model.VideoLink = video.VideoLink;
                model.ThumbLink = video.ThumbLink;
                model.AccessLevel = video.AccessLevel;
                model.Author = video.Author;
                model.AccessLevelListEn[video.AccessLevel].Selected = true;
                model.AccessLevelListDa[video.AccessLevel].Selected = true;
                model.AccessLevelListDe[video.AccessLevel].Selected = true;
                model.CommentThreadNumber = video.CommentThreadNumber;
                model.Tags = video.Tags;
                model.VideoNumber = 1;
                model.VideoCount = 1;
                model.CommentsList = await _dataService.GetCommentsList(video.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == video.CommentThreadNumber).ToListAsync();
                model.Location = video.Location;
                model.Longtitude = video.Longtitude;
                model.Latitude = video.Latitude;
                model.Altitude = video.Latitude;
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Video> videosList = await _dataService.GetVideosList(video.ProgenyId); // await _context.VideoDb.Where(p => p.ProgenyId == video.ProgenyId && p.AccessLevel >= accessLevel).ToListAsync();
                videosList = videosList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
                if (videosList.Any())
                {
                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Video vid in videosList)
                    {
                        if (vid.VideoId == video.VideoId)
                        {
                            currentIndex = indexer;
                        }
                        indexer++;
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
                        int tempVal = model.NextVideo;
                        model.NextVideo = model.PrevVideo;
                        model.PrevVideo = tempVal;
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
                    tagItems = tagItems + "]";
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
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Video> videosList = await _dataService.GetVideosList(id); // await _context.VideoDb.Where(v => v.ProgenyId == id && v.AccessLevel >= accessLevel).ToListAsync();
            videosList = videosList.Where(v => v.AccessLevel >= accessLevel).ToList();
            if (videosList.Any())
            {
                foreach (Video video in videosList)
                {
                    video.Comments = await _dataService.GetCommentsList(video.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == video.CommentThreadNumber).ToListAsync();
                }
            }
            return Ok(videosList);
        }

        
        // GET api/videos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            Video result = await _dataService.GetVideo(id); // await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);

            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
            Video result = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoLink == videoLink && v.ProgenyId == progenyId);

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
            UserAccess userAccess = _progenyDbContext.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == model.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            Video vid = await _context.VideoDb.SingleOrDefaultAsync(v =>
                v.VideoLink == model.VideoLink && v.ProgenyId == model.ProgenyId);
            if (vid == null)
            {
                CommentThread commentThread = new CommentThread();
                await _context.CommentThreadsDb.AddAsync(commentThread);
                await _context.SaveChangesAsync();
                _context.CommentThreadsDb.Update(commentThread);
                await _context.SaveChangesAsync();
                model.CommentThreadNumber = commentThread.Id;

                await _context.VideoDb.AddAsync(model);
                await _context.SaveChangesAsync();
                await _dataService.SetVideo(model.VideoId);

                return Ok(model);
            }

            model = vid;

            return Ok(model);
        }

        // PUT api/videos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Video value)
        {
            Video video = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);

            // Todo: more validation of the values
            if (video == null)
            {
                return NotFound();
            }

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == video.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
            _context.VideoDb.Update(video);
            await _context.SaveChangesAsync();
            await _dataService.SetVideo(video.VideoId);

            return Ok(video);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Video video = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            if (video != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                    u.ProgenyId == video.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                List<Comment> comments = _context.CommentsDb
                    .Where(c => c.CommentThreadNumber == video.CommentThreadNumber).ToList();
                if (comments.Any())
                {
                    _context.CommentsDb.RemoveRange(comments);
                    _context.SaveChanges();

                    foreach (Comment deletedComment in comments)
                    {
                        await _dataService.RemoveComment(deletedComment.CommentId, deletedComment.CommentThreadNumber);
                    }
                }

                CommentThread cmntThread =
                    _context.CommentThreadsDb.SingleOrDefault(c => c.Id == video.CommentThreadNumber);
                if (cmntThread != null)
                {
                    _context.CommentThreadsDb.Remove(cmntThread);
                    _context.SaveChanges();
                    await _dataService.RemoveCommentsList(video.CommentThreadNumber);
                }
                _context.VideoDb.Remove(video);
                await _context.SaveChangesAsync();
                await _dataService.RemoveVideo(video.VideoId, video.ProgenyId);
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
            Video result = await _dataService.GetVideo(id); // await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (tagFilter != "")
            {
                allItems = await _dataService.GetVideosList(progenyId); // await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
            }
            else
            {
                allItems = await _dataService.GetVideosList(progenyId); // await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
            List<string> tagsList = new List<string>();
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

            var itemsOnPage = allItems.Skip(pageSize * (pageIndex - 1)).Take(pageSize).ToList();

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await _dataService.GetCommentsList(vid.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == vid.CommentThreadNumber).ToListAsync();
            }
            VideoPageViewModel model = new VideoPageViewModel();
            model.VideosList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
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
