using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaMediaApi.Models.ViewModels;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KinaUnaMediaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicAccessController(ImageStore imageStore, IPicturesService picturesService, IVideosService videosService, ICommentsService commentsService, IConfiguration configuration)
        : ControllerBase
    {
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> RandomPictureMobile(int progenyId, int accessLevel)
        {
            List<Picture> picturesList = await picturesService.GetPicturesList(Constants.DefaultChildId);
            picturesList = picturesList.Where(p => p.AccessLevel >= 5).ToList();
            if (picturesList.Count != 0)
            {
                Random r = new();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                
                if (picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return Ok(picture);
                
                picture.PictureLink = imageStore.UriFor(picture.PictureLink);
                picture.PictureLink1200 = imageStore.UriFor(picture.PictureLink1200);
                picture.PictureLink600 = imageStore.UriFor(picture.PictureLink600);

                return Ok(picture);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),
                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await picturesService.GetPicture(id);
            if (result != null && result.ProgenyId == Constants.DefaultChildId)
            {
                if (result.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return Ok(result);

                result.PictureLink = imageStore.UriFor(result.PictureLink);
                result.PictureLink1200 = imageStore.UriFor(result.PictureLink1200);
                result.PictureLink600 = imageStore.UriFor(result.PictureLink600);
                return Ok(result);

            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await picturesService.GetPicturesList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5 && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }
            else
            {
                allItems = await picturesService.GetPicturesList(2);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.PictureTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = [];
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
                if (string.IsNullOrEmpty(pic.Tags)) continue;

                List<string> pvmTags = [.. pic.Tags.Split(',')];
                foreach (string tagstring in pvmTags)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }

            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                if (!pic.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink = imageStore.UriFor(pic.PictureLink);
                }
                if (!pic.PictureLink1200.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink1200 = imageStore.UriFor(pic.PictureLink1200);
                }
                if (!pic.PictureLink600.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
                }
            }
            PicturePageViewModel model = new()
            {
                PicturesList = itemsOnPage,
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
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            if (picture.ProgenyId != Constants.DefaultChildId)
            {
                return NotFound();
            }

            PictureViewModel model = new()
            {
                PictureId = picture.PictureId,
                PictureTime = picture.PictureTime,
                ProgenyId = picture.ProgenyId,
                Owners = picture.Owners,
                PictureLink = picture.PictureLink1200
            };
            if (!model.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                model.PictureLink = imageStore.UriFor(model.PictureLink);
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
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
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
                    
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
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
                    (model.NextPicture, model.PrevPicture) = (model.PrevPicture, model.NextPicture);
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

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVideoMobile(int id)
        {
            Video result = await videosService.GetVideo(id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            // Todo: Create default video item
            result = await videosService.GetVideo(204);
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> VideoPageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (tagFilter != "")
            {
                allItems = await videosService.GetVideosList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5 && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = await videosService.GetVideosList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.VideoTime)];
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
        public async Task<IActionResult> VideoViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Video video = await videosService.GetVideo(id);

            if (video != null)
            {
                if (video.ProgenyId != Constants.DefaultChildId)
                {
                    return NotFound();
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

            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> PictureTagsList(int id)
        {
            string tagListString = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(id);
            if (pictureList.Count != 0)
            {
                foreach (Picture pic in pictureList)
                {
                    if (string.IsNullOrEmpty(pic.Tags)) continue;
                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }
            else
            {
                return Ok(tagListString);
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

            tagListString = tagItems;
            return Ok(tagListString);
        }
    }
}