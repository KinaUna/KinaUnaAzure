using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PicturesController : ControllerBase
    {
        private readonly IImageStore _imageStore;
        private readonly IPicturesService _picturesService;
        private readonly IVideosService _videosService;
        private readonly ICommentsService _commentsService;
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;
        private readonly ITimelineService _timelineService;

        public PicturesController(IImageStore imageStore, IAzureNotifications azureNotifications, IPicturesService picturesService, IVideosService videosService, ICommentsService commentsService,
            IProgenyService progenyService, IUserInfoService userInfoService, IUserAccessService userAccessService, IWebNotificationsService webNotificationsService, ITimelineService timelineService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _picturesService = picturesService;
            _videosService = videosService;
            _commentsService = commentsService;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _webNotificationsService = webNotificationsService;
            _timelineService = timelineService;
        }

        // GET api/pictures/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery]int pageSize = 16, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (accessLevel < userAccess?.AccessLevel)
            {
                accessLevel = userAccess.AccessLevel;
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems; 
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await _picturesService.GetPicturesList(progenyId);
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper()))
                    .OrderBy(p => p.PictureTime).ToList();
            }
            else
            {
                allItems = await _picturesService.GetPicturesList(progenyId);
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
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
                if (!string.IsNullOrEmpty(pic.Tags))
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
            
            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await _commentsService.GetCommentsList(pic.CommentThreadNumber);
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
        public async Task<IActionResult> PictureViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1, [FromQuery] string tagFilter = "")
        {
            Picture picture = await _picturesService.GetPicture(id); 
            
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail); 

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                if (accessLevel < userAccess?.AccessLevel)
                {
                    accessLevel = userAccess.AccessLevel;
                }
                
                PictureViewModel model = new PictureViewModel();
                model.SetPicturePropertiesFromPictureItem(picture);
                model.PictureNumber = 1;
                model.PictureCount = 1;
                model.CommentsList = await _commentsService.GetCommentsList(picture.CommentThreadNumber); 
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Picture> pictureList = await _picturesService.GetPicturesList(picture.ProgenyId); 
                pictureList = pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
                if (pictureList.Any())
                {
                    if (!string.IsNullOrEmpty(tagFilter))
                    {
                        pictureList = pictureList.Where(p => p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper()))
                            .OrderBy(p => p.PictureTime).ToList();
                    }

                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Picture pic in pictureList)
                    {
                        if (pic.PictureId == picture.PictureId)
                        {
                            currentIndex = indexer;
                        }
                        indexer++;

                        if (!string.IsNullOrEmpty(pic.Tags))
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
                    if(currentIndex > 0)
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
                
                model.SetTagsList(tagsList);
                return Ok(model);
            }

            return NotFound();
        }

        // GET api/pictures/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> Progeny(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await _picturesService.GetPicturesList(id); 
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            
            if (picturesList.Any())
            {
                foreach (Picture pic in picturesList)
                {
                    pic.Comments = await _commentsService.GetCommentsList(pic.CommentThreadNumber);
                    pic.PictureLink = _imageStore.UriFor(pic.PictureLink);
                    pic.PictureLink1200 = _imageStore.UriFor(pic.PictureLink1200);
                    pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
                }

                return Ok(picturesList);
            }
            
            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();

            picturesList.Add(tempPicture);
            return Ok(picturesList);
        }

        // GET api/pictures/bylink/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ByLink(string id)
        {
            Picture picture = await _picturesService.GetPictureByLink(id);
            if (picture != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                picture.Comments = await _commentsService.GetCommentsList(picture.CommentThreadNumber); 
                
                return Ok(picture);
            }

            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();

            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPicture(int id)
        {
            Picture result = await _picturesService.GetPicture(id); 
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); 

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }
                
                return Ok(result);
            }

            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();

            return Ok(tempPicture);
        }

        // POST api/pictures
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Picture model)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(model.ProgenyId, userEmail);
            
            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            model = await _picturesService.ProcessPicture(model);

            CommentThread commentThread = await _commentsService.AddCommentThread();
            model.CommentThreadNumber = commentThread.Id;

            model.Author = User.GetUserId();

            model = await _picturesService.AddPicture(model);
           
            await _picturesService.SetPictureInCache(model.PictureId);
            await _commentsService.SetCommentsList(model.CommentThreadNumber);

            Progeny progeny = await _progenyService.GetProgeny(model.ProgenyId);
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "New Photo added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new photo for " + progeny.NickName;
            
            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.CopyPicturePropertiesForAdd(model);
            _ = await _timelineService.AddTimeLineItem(timeLineItem);
            
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendPictureNotification(model, userInfo, notificationTitle);

            return Ok(model);
        }

        // PUT api/pictures/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Picture value)
        {
            Picture picture = await _picturesService.GetPicture(id);

            // Todo: more validation of the values
            if (picture == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }
            
            picture = await _picturesService.UpdatePicture(value);
            
            await _picturesService.SetPictureInCache(picture.PictureId);
            await _commentsService.SetCommentsList(picture.CommentThreadNumber);

            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(picture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
            if (timeLineItem != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(picture.ProgenyId);
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());

                string notificationTitle = "Photo Edited for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " edited a photo for " + progeny.NickName;

                timeLineItem.CopyPicturePropertiesForUpdate(picture);
                
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);

                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendPictureNotification(picture, userInfo, notificationTitle);
            }

            return Ok(picture);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Picture picture = await _picturesService.GetPicture(id);
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                List<Comment> comments = await _commentsService.GetCommentsList(picture.CommentThreadNumber);
                if (comments.Any())
                {
                    foreach (Comment deletedComment in comments)
                    {
                        await _commentsService.DeleteComment(deletedComment);
                        await _commentsService.RemoveComment(deletedComment.CommentId, deletedComment.CommentThreadNumber);
                    }
                }

                CommentThread cmntThread = await _commentsService.GetCommentThread(picture.CommentThreadNumber);
                if (cmntThread != null)
                {
                    await _commentsService.RemoveCommentsList(picture.CommentThreadNumber);
                }

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(picture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
                if (timeLineItem != null)
                {
                    await _timelineService.DeleteTimeLineItem(timeLineItem);
                }
                
                await _imageStore.DeleteImage(picture.PictureLink);
                await _imageStore.DeleteImage(picture.PictureLink600);
                await _imageStore.DeleteImage(picture.PictureLink1200);

                await _picturesService.DeletePicture(picture);
                await _picturesService.RemovePictureFromCache(picture.PictureId, picture.ProgenyId);

                if (timeLineItem != null)
                {
                    Progeny progeny = await _progenyService.GetProgeny(picture.ProgenyId);
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
                    string notificationTitle = "Photo deleted for " + progeny.NickName;
                    string notificationMessage = userInfo.FirstName + " " + userInfo.MiddleName + " " + userInfo.LastName + " deleted a photo for " + progeny.NickName;

                    picture.AccessLevel = timeLineItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendPictureNotification(picture, userInfo, notificationTitle);
                }
                
                return NoContent();
            }

            return NotFound();

        }

        // GET api/pictures/random/[Progeny id]?accessLevel=5
        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> Random(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail); 

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await _picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Any())
            {
                Random r = new Random();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];

                return Ok(picture);
            }
            
            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();
            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> RandomMobile(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await _picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Any())
            {
                Random r = new Random();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                if (!picture.PictureLink.ToLower().StartsWith("http"))
                {
                    picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                    picture.PictureLink1200 = _imageStore.UriFor(picture.PictureLink1200);
                    picture.PictureLink600 = _imageStore.UriFor(picture.PictureLink600);
                }
                
                return Ok(picture);
            }
            
            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();

            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await _picturesService.GetPicture(id);
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                if (!result.PictureLink.ToLower().StartsWith("http"))
                {
                    result.PictureLink = _imageStore.UriFor(result.PictureLink);
                    result.PictureLink1200 = _imageStore.UriFor(result.PictureLink1200);
                    result.PictureLink600 = _imageStore.UriFor(result.PictureLink600);
                }
                return Ok(result);
            }

            
            Picture tempPicture = new Picture();
            tempPicture.ApplyPlacholderProperties();

            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
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

            List<Picture> allItems = await _picturesService.GetPicturesList(progenyId);
            List<string> tagsList = new List<string>();
            foreach (Picture pic in allItems)
            {
                if (!string.IsNullOrEmpty(pic.Tags))
                {
                    List<string> pictureTagsList = pic.Tags.Split(',').ToList();
                    foreach (string tagstring in pictureTagsList)
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
                
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToList();
            }
            else
            {
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            
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
                
            }

            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await _commentsService.GetCommentsList(pic.CommentThreadNumber);
                pic.PictureLink = _imageStore.UriFor(pic.PictureLink);
                pic.PictureLink1200 = _imageStore.UriFor(pic.PictureLink1200);
                pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
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
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel=5, [FromQuery] int sortBy = 1)
        {
            Picture picture = await _picturesService.GetPicture(id);

            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null)
                {
                    return Unauthorized();
                }

                PictureViewModel model = new PictureViewModel();
                model.SetPicturePropertiesFromPictureItem(picture);

                model.PictureLink = _imageStore.UriFor(model.PictureLink);
                model.PictureNumber = 1;
                model.PictureCount = 1;
                model.CommentsList = await _commentsService.GetCommentsList(picture.CommentThreadNumber);
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Picture> pictureList = await _picturesService.GetPicturesList(picture.ProgenyId);
                pictureList = pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime).ToList();
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
                
                model.SetTagsList(tagsList);

                return Ok(model);
            }

            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> PictureViewModelMaui(int id)
        {
            Picture picture = await _picturesService.GetPicture(id);

            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null)
                {
                    return Unauthorized();
                }

                PictureViewModel model = new PictureViewModel();
                model.SetPicturePropertiesFromPictureItem(picture);
                model.PictureLink = _imageStore.UriFor(model.PictureLink);
                model.PictureNumber = 1;
                model.PictureCount = 1;
                model.CommentsList = await _commentsService.GetCommentsList(picture.CommentThreadNumber);
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Picture> pictureList = await _picturesService.GetPicturesList(picture.ProgenyId);
                pictureList = pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime).ToList();
                if (pictureList.Any())
                {
                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Picture pictureItem in pictureList)
                    {
                        if (pictureItem.PictureId == picture.PictureId)
                        {
                            currentIndex = indexer;
                        }
                        indexer++;
                        if (!string.IsNullOrEmpty(pictureItem.Tags))
                        {
                            List<string> pictureTagsList = pictureItem.Tags.Split(',').ToList();
                            foreach (string tagstring in pictureTagsList)
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

                    int tempVal = model.NextPicture;
                    model.NextPicture = model.PrevPicture;
                    model.PrevPicture = tempVal;
                }
                
                model.SetTagsList(tagsList);

                return Ok(model);
            }

            return NotFound();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadPicture([FromForm]IFormFile file)
        {
            string pictureLink;
            await using (Stream stream = file.OpenReadStream())
            {
                pictureLink = await _imageStore.SaveImage(stream);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProgenyPicture([FromForm]IFormFile file)
        {
            string pictureLink = await _picturesService.ProcessProgenyPicture(file);
            
            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture([FromForm]IFormFile file)
        {
            string pictureLink = await _picturesService.ProcessProfilePicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadFriendPicture([FromForm]IFormFile file)
        {
            string pictureLink = await _picturesService.ProcessFriendPicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadContactPicture([FromForm]IFormFile file)
        {
            string pictureLink = await _picturesService.ProcessContactPicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadNoteImage([FromForm] IFormFile file)
        {
            string pictureLink;
            using (Stream stream = file.OpenReadStream())
            {
                pictureLink = await _imageStore.SaveImage(stream, BlobContainers.Notes);
                pictureLink = _imageStore.UriFor(pictureLink, BlobContainers.Notes);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]/{id}")]
        [HttpGet]
        public IActionResult GetProfilePicture(string id)
        {
            string result = _imageStore.UriFor(id, "profiles");

            if (string.IsNullOrEmpty(result))
            {
                result = Constants.ProfilePictureUrl;
            }

            return Ok(result);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            
            List<Picture> allPictures = await _picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            
            List<string> autoSuggestList = new List<string>();
            
            foreach (Picture picture in allPictures)
            {
                if (!string.IsNullOrEmpty(picture.Location))
                {
                    if (!autoSuggestList.Contains(picture.Location))
                    {
                        autoSuggestList.Add(picture.Location);
                    }
                }
            }

            List<Video> allVideos = await _videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (!string.IsNullOrEmpty(video.Location))
                {
                    if (!autoSuggestList.Contains(video.Location))
                    {
                        autoSuggestList.Add(video.Location);
                    }
                }
            }
            
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }


            List<Picture> allPictures = await _picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = new List<string>();
            
            foreach (Picture picture in allPictures)
            {
                if (!string.IsNullOrEmpty(picture.Tags))
                {
                    List<string> tagsList = picture.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Video> allVideos = await _videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            
            foreach (Video video in allVideos)
            {
                if (!string.IsNullOrEmpty(video.Tags))
                {
                    List<string> tagsList = video.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }

        

        // Download pictures to StorageBlob from Url
        [HttpGet]
        [Route("[action]/{pictureId}")]
        public async Task<IActionResult> DownloadPicture(int pictureId)
        {
            Picture picture = await _picturesService.GetPicture(pictureId);
            if (picture != null && picture.PictureLink.ToLower().StartsWith("http"))
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink))
                {
                    picture.PictureLink = await _imageStore.SaveImage(stream);
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink600))
                {
                    picture.PictureLink600 = await _imageStore.SaveImage(stream);
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink1200))
                {
                    picture.PictureLink1200 = await _imageStore.SaveImage(stream);
                }

                picture = await _picturesService.UpdatePicture(picture);
                return Ok(picture);
            }
            else
            {
                return NotFound();
            }
        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            return streamToReadFrom;
        }
    }
}
