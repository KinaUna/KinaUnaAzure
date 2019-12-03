using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaMediaApi.Models.ViewModels;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaMediaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicAccessController : ControllerBase
    {
        private readonly MediaDbContext _context;
        private readonly ImageStore _imageStore;
        private readonly IDataService _dataService;

        public PublicAccessController(MediaDbContext context, ImageStore imageStore, IDataService dataService)
        {
            _context = context;
            _imageStore = imageStore;
            _dataService = dataService;
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> RandomPictureMobile(int progenyId, int accessLevel)
        {
            List<Picture> picturesList = await _dataService.GetPicturesList(Constants.DefaultChildId); // await _context.PicturesDb.Where(p => p.ProgenyId == 2 && p.AccessLevel >= 5).ToListAsync();
            picturesList = picturesList.Where(p => p.AccessLevel >= 5).ToList();
            if (picturesList.Any())
            {
                Random r = new Random();
                var pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                if (!picture.PictureLink.ToLower().StartsWith("http"))
                {
                    picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                    picture.PictureLink1200 = _imageStore.UriFor(picture.PictureLink1200);
                    picture.PictureLink600 = _imageStore.UriFor(picture.PictureLink600);
                }

                return Ok(picture);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await _dataService.GetPicture(id); // await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            if (result != null)
            {
                if (result.ProgenyId == Constants.DefaultChildId)
                {
                    if (!result.PictureLink.ToLower().StartsWith("http"))
                    {
                        result.PictureLink = _imageStore.UriFor(result.PictureLink);
                        result.PictureLink1200 = _imageStore.UriFor(result.PictureLink1200);
                        result.PictureLink600 = _imageStore.UriFor(result.PictureLink600);
                    }
                    return Ok(result);
                }
                
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await _dataService.GetPicturesList(Constants.DefaultChildId); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == 2 && p.AccessLevel >= 5 && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= 5 && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToList();
            }
            else
            {
                allItems = await _dataService.GetPicturesList(2); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == 2 && p.AccessLevel >= 5).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.PictureTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = new List<string>();
            foreach (Picture pic in allItems)
            {
                if (sortBy == 1)
                {
                    pic.PictureNumber = picCount - pictureCounter + 1;
                }
                else
                {
                    pic.PictureNumber = pictureCounter;
                }

                pictureCounter++;
                if (!String.IsNullOrEmpty(pic.Tags))
                {
                    List<string> pvmTags = pic.Tags.Split(',').ToList();
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await _dataService.GetCommentsList(pic.CommentThreadNumber); // await _context.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == pic.CommentThreadNumber).ToListAsync();
                if (!pic.PictureLink.ToLower().StartsWith("http"))
                {
                    pic.PictureLink = _imageStore.UriFor(pic.PictureLink);
                }
                if (!pic.PictureLink1200.ToLower().StartsWith("http"))
                {
                    pic.PictureLink1200 = _imageStore.UriFor(pic.PictureLink1200);
                }
                if (!pic.PictureLink600.ToLower().StartsWith("http"))
                {
                    pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
                }
            }
            PicturePageViewModel model = new PicturePageViewModel();
            model.PicturesList = itemsOnPage;
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
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Picture picture = await _dataService.GetPicture(id); // await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);

            if (picture != null)
            {
                if (picture.ProgenyId != Constants.DefaultChildId)
                {
                    return NotFound();
                }

                PictureViewModel model = new PictureViewModel();
                model.PictureId = picture.PictureId;
                model.PictureTime = picture.PictureTime;
                model.ProgenyId = picture.ProgenyId;
                model.Owners = picture.Owners;
                model.PictureLink = picture.PictureLink1200;
                if (!model.PictureLink.ToLower().StartsWith("http"))
                {
                    model.PictureLink = _imageStore.UriFor(model.PictureLink);
                }
                model.AccessLevel = picture.AccessLevel;
                model.Author = picture.Author;
                model.CommentThreadNumber = picture.CommentThreadNumber;
                model.Tags = picture.Tags;
                model.Location = picture.Location;
                model.Latitude = picture.Latitude;
                model.Longtitude = picture.Longtitude;
                model.Altitude = picture.Altitude;
                model.PictureNumber = 1;
                model.PictureCount = 1;
                model.CommentsList = await _dataService.GetCommentsList(picture.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == picture.CommentThreadNumber).ToListAsync();
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Picture> pictureList = await _dataService.GetPicturesList(picture.ProgenyId); // await _context.PicturesDb.AsNoTracking()
                pictureList = pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
                if (pictureList.Any())
                {
                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Picture pic in pictureList)
                    {
                        if (pic.PictureId == picture.PictureId)
                        {
                            currentIndex = indexer;
                        }
                        indexer++;
                        if (!String.IsNullOrEmpty(pic.Tags))
                        {
                            List<string> pvmTags = pic.Tags.Split(',').ToList();
                            foreach (string tagstring in pvmTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                    model.PictureNumber = currentIndex + 1;
                    model.PictureCount = pictureList.Count;
                    if (currentIndex > 0)
                    {
                        model.PrevPicture = pictureList[currentIndex - 1].PictureId;
                    }
                    else
                    {
                        model.PrevPicture = pictureList.Last().PictureId;
                    }

                    if (currentIndex + 1 < pictureList.Count)
                    {
                        model.NextPicture = pictureList[currentIndex + 1].PictureId;
                    }
                    else
                    {
                        model.NextPicture = pictureList.First().PictureId;
                    }

                    if (sortBy == 1)
                    {
                        int tempVal = model.NextPicture;
                        model.NextPicture = model.PrevPicture;
                        model.PrevPicture = tempVal;
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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVideoMobile(int id)
        {
            Video result = await _dataService.GetVideo(id); // await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            // Todo: Create default video item
            result = await _context.VideoDb.SingleOrDefaultAsync(v => v.VideoId == 204 );
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> VideoPageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (tagFilter != "")
            {
                allItems = await _dataService.GetVideosList(Constants.DefaultChildId); // await _context.VideoDb.Where(p => p.ProgenyId == 2 && p.AccessLevel >= 5 && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= 5 && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.VideoTime).ToList();
            }
            else
            {
                allItems = await _dataService.GetVideosList(Constants.DefaultChildId); //await _context.VideoDb.Where(p => p.ProgenyId == 2 && p.AccessLevel >= 5).OrderBy(p => p.VideoTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.VideoTime).ToList();
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
                vid.Comments = await _dataService.GetCommentsList(vid.CommentThreadNumber);
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
        public async Task<IActionResult> VideoViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Video video = await _dataService.GetVideo(id);

            if (video != null)
            {
                if (video.ProgenyId != Constants.DefaultChildId)
                {
                    return NotFound();
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
                model.CommentsList = await _dataService.GetCommentsList(video.CommentThreadNumber);
                model.Location = video.Location;
                model.Longtitude = video.Longtitude;
                model.Latitude = video.Latitude;
                model.Altitude = video.Latitude;
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Video> videosList = await _dataService.GetVideosList(video.ProgenyId);
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

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> PictureTagsList(int id)
        {
            string tagListString = "";
            List<string> tagsList = new List<string>();
            List<Picture> pictureList = await _dataService.GetPicturesList(id); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == id).ToListAsync();
            if (pictureList.Any())
            {
                foreach (Picture pic in pictureList)
                {
                    if (!String.IsNullOrEmpty(pic.Tags))
                    {
                        List<string> pvmTags = pic.Tags.Split(',').ToList();
                        foreach (string tagstring in pvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }
                }
            }
            else
            {
                return Ok(tagListString);
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

            tagListString = tagItems;
            return Ok(tagListString);
        }
    }
}