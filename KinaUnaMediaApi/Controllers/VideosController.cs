using KinaUnaMediaApi.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        private readonly MediaDbContext _context;
        public VideosController(MediaDbContext context)
        {
            _context = context;
        }

        // GET api/videos
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Video> resultList = await _context.VideoDb.ToListAsync();
            return Ok(resultList);
        }

        // GET api/videos/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = 2, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems; 
            if (tagFilter != "")
            {
                allItems = await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToListAsync();
            }
            else
            {
                allItems = await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToListAsync();
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
                vid.Comments = await _context.CommentsDb.Where(c => c.CommentThreadNumber == vid.CommentThreadNumber).ToListAsync();
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
            Video video = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            if (video != null)
            {
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
                model.CommentsList = await _context.CommentsDb.Where(c => c.CommentThreadNumber == video.CommentThreadNumber).ToListAsync();
                model.Location = video.Location;
                model.Longtitude = video.Longtitude;
                model.Latitude = video.Latitude;
                model.Altitude = video.Latitude;
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Video> videosList = await _context.VideoDb
                    .Where(p => p.ProgenyId == video.ProgenyId && p.AccessLevel >= accessLevel).ToListAsync();
                if (videosList.Any())
                {
                    foreach (Video vid in videosList)
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
                    videosList = videosList.OrderBy(p => p.VideoTime).ToList();
                    int currentIndex = videosList.IndexOf(video);
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
            List<Video> videosList = await _context.VideoDb.Where(v => v.ProgenyId == id && v.AccessLevel >= accessLevel).ToListAsync();
            if (videosList.Any())
            {
                foreach (Video video in videosList)
                {
                    video.Comments = await _context.CommentsDb.Where(c => c.CommentThreadNumber == video.CommentThreadNumber).ToListAsync();
                }
            }
            return Ok(videosList);
        }

        
        // GET api/videos/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            Video result = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            
            return Ok(result);
        }

        [HttpGet("[action]/{videoLink}/{progenyId}")]
        public async Task<IActionResult> ByLink(string videoLink, int progenyId)
        {
            Video result = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoLink == videoLink && v.ProgenyId == progenyId);

            return Ok(result);
        }

        // POST api/videos
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Video model)
        {
            Video vid = await _context.VideoDb.SingleOrDefaultAsync(v =>
                v.VideoLink == model.VideoLink && v.ProgenyId == model.ProgenyId);
            if (vid == null)
            {
                CommentThread commentThread = new CommentThread();
                await _context.CommentThreadsDb.AddAsync(commentThread);
                await _context.SaveChangesAsync();
                commentThread.CommentThreadId = commentThread.Id;
                _context.CommentThreadsDb.Update(commentThread);
                await _context.SaveChangesAsync();
                model.CommentThreadNumber = commentThread.CommentThreadId;

                await _context.VideoDb.AddAsync(model);
                await _context.SaveChangesAsync();

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

            return Ok(video);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Video video = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            if (video != null)
            {
                // Todo: Delete content associated with picture, i.e. comments.

                _context.VideoDb.Remove(video);
                await _context.SaveChangesAsync();
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
            Video result = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);

            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = 2, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (tagFilter != "")
            {
                allItems = await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToListAsync();
            }
            else
            {
                allItems = await _context.VideoDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime).ToListAsync();
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
                vid.Comments = await _context.CommentsDb.Where(c => c.CommentThreadNumber == vid.CommentThreadNumber).ToListAsync();
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
