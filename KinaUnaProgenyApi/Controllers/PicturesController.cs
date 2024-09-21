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
    /// <summary>
    /// API endpoints for Pictures.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="azureNotifications"></param>
    /// <param name="picturesService"></param>
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
    public class PicturesController(
        IImageStore imageStore,
        IAzureNotifications azureNotifications,
        IPicturesService picturesService,
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
        /// Gets a list of Picture entities for a page for a given ProgenyId, AccessLevel, and Tag.
        /// Includes Comments for each Picture entity.
        /// </summary>
        /// <param name="pageSize">The number of Picture elements per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId to get Picture entities for.</param>
        /// <param name="accessLevel">The current user's access level for this Progeny.</param>
        /// <param name="tagFilter">Only include Picture entities with a Tag property that contains the tagFilter string. If empty, include all Picture entities.</param>
        /// <param name="sortBy">Sort order, 0 = oldest first, 1 = newest first.</param>
        /// <returns>PicturePageViewModel with the list of Picture objects that belong on the page, and other properties for the page.</returns>
        // GET api/pictures/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery] int pageSize = 16, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

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

            // Todo: Refactor this to use a separate method in the service.
            List<Picture> allItems = await picturesService.GetPicturesWithTag(progenyId, tagFilter);
            allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];

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

                List<string> picturePageViewModelTagsList = [.. pic.Tags.Split(',')];
                foreach (string tagString in picturePageViewModelTagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        tagsList.Add(trimmedTag);
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
            }

            PicturePageViewModel model = new()
            {
                PicturesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter,
                TagsList = ""
            };
            foreach (string tagString in tagsList)
            {
                model.TagsList = model.TagsList + tagString + ",";
            }
            model.TagsList = model.TagsList.TrimEnd(',');

            return Ok(model);
        }

        /// <summary>
        /// Gets a PictureViewModel for a Picture entity with the provided PictureId.
        /// </summary>
        /// <param name="id">The PictureId of the entity to get.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny the Picture belongs to.</param>
        /// <param name="sortBy">Sort order, 0=oldest first, 1= newest first.</param>
        /// <param name="tagFilter">Only include Picture items where the Tag property contains the tagFilter string. If tagFilter is an empty string, include all Picture items.</param>
        /// <returns>PictureViewModel for the Picture and current user.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> PictureViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1, [FromQuery] string tagFilter = "")
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (accessLevel < userAccess?.AccessLevel)
            {
                accessLevel = userAccess.AccessLevel;
            }

            PictureViewModel model = new();
            model.SetPicturePropertiesFromPictureItem(picture);
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
            {
                if (!string.IsNullOrEmpty(tagFilter))
                {
                    pictureList = [.. pictureList.Where(p => p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
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

                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagString in pvmTags)
                    {
                        string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                        if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                        {
                            tagsList.Add(trimmedTag);
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

            model.SetTagsList(tagsList);
            return Ok(model);

        }

        /// <summary>
        /// Gets a simplified PictureViewModel for a Picture entity with the provided PictureId.
        /// PictureNumber, PictureCount, CommentsList, and TagsList are not included.
        /// </summary>
        /// <param name="id">The PictureId of the Picture entity to get a PictureViewModel for.</param>
        /// <returns>PictureViewModel for the Picture and the current user.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> PictureElement(int id)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);
            
            if ((userAccess == null && picture.ProgenyId != Constants.DefaultChildId) || (userAccess != null && userAccess.AccessLevel > picture.AccessLevel && picture.ProgenyId != Constants.DefaultChildId ))
            {
                return Unauthorized();
            }

            PictureViewModel model = new();
            model.SetPicturePropertiesFromPictureItem(picture);
            model.PictureNumber = 0;
            model.PictureCount = 0;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            return Ok(model);

        }

        /// <summary>
        /// Gets a list of all Picture entities for a given ProgenyId and AccessLevel.
        /// Includes comments.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Picture objects.</returns>
        // GET api/pictures/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> Progeny(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(id);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();

            if (picturesList.Count != 0)
            {
                foreach (Picture pic in picturesList)
                {
                    pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                    pic.PictureLink = Constants.ProgenyApiUrl + pic.GetPictureUrl(0); // imageStore.UriFor(pic.PictureLink);
                    pic.PictureLink1200 = Constants.ProgenyApiUrl + pic.GetPictureUrl(1200); // imageStore.UriFor(pic.PictureLink1200);
                    pic.PictureLink600 = Constants.ProgenyApiUrl + pic.GetPictureUrl(600); //imageStore.UriFor(pic.PictureLink600);

                }

                return Ok(picturesList);
            }

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            picturesList.Add(tempPicture);
            return Ok(picturesList);
        }

        /// <summary>
        /// Gets a list of all Picture entities for a given ProgenyId and AccessLevel.
        /// Does not include comments.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get pictures for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>List of all Pictures for the Progeny that the user have access to.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> ProgenyPicturesList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(id);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();

            if (picturesList.Count != 0)
            {
                return Ok(picturesList);
            }

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            picturesList.Add(tempPicture);
            return Ok(picturesList);
        }


        /// <summary>
        /// Gets a picture entity by the PictureLink property.
        /// Includes comments.
        /// </summary>
        /// <param name="id">The PictureLink of the Picture to get.</param>
        /// <returns>Picture object with the given PictureLink.</returns>
        // GET api/pictures/bylink/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ByLink(string id)
        {
            Picture picture = await picturesService.GetPictureByLink(id);
            if (picture != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                picture.Comments = await commentsService.GetCommentsList(picture.CommentThreadNumber);

                return Ok(picture);
            }

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets a Picture entity by the PictureId property.
        /// Does not include comments.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get.</param>
        /// <returns>Picture object with the given PictureId. If the user doesn't have the required access level, Unauthorized is returned.</returns>
        // GET api/pictures/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPicture(int id)
        {
            Picture result = await picturesService.GetPicture(id);
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                return Ok(result);
            }

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets the image file for a Picture entity with the given PictureId and size.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get the file for.</param>
        /// <param name="size">The size of the image to get a file for, 600 = 600 pixels wide, 1200 = 1200 pixes wide, any other number gets the original image file.</param>
        /// <returns>FileContentResult with the image file, if the current user has the access rights for the Picture, else a placeholder image file.</returns>
        [AllowAnonymous]
        public async Task<FileContentResult> File([FromQuery] int id, [FromQuery] int size)
        {
            Picture picture = await picturesService.GetPicture(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId || picture.PictureId == 0)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream(Constants.PlaceholderImageLink);
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/png");
            }
            
            string fileName = picture.PictureLink;
            if (size == 600)
            {
                fileName = picture.PictureLink600;
            }
            else if (size == 1200)
            {
                fileName = picture.PictureLink1200;
            }

            MemoryStream fileContent = await imageStore.GetStream(fileName);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, picture.GetPictureFileContentType());
        }

        /// <summary>
        /// Adds a new Picture entity to the database.
        /// Also adds a new CommentThread entity and TimeLineItem entity for the Picture and sends notifications to users who have access to it.
        /// </summary>
        /// <param name="model">Picture object to add.</param>
        /// <returns>The Picture object added.</returns>
        // POST api/pictures
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Picture model)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(model.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            model = await picturesService.ProcessPicture(model);

            CommentThread commentThread = await commentsService.AddCommentThread();
            model.CommentThreadNumber = commentThread.Id;

            model.Author = User.GetUserId();

            model = await picturesService.AddPicture(model);
            
            await commentsService.SetCommentsList(model.CommentThreadNumber);

            Progeny progeny = await progenyService.GetProgeny(model.ProgenyId);
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "New Photo added for " + progeny.NickName; // Todo: Localize.
            string notificationMessage = userInfo.FullName() + " added a new photo for " + progeny.NickName; // Todo: Localize.

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyPicturePropertiesForAdd(model);
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendPictureNotification(model, userInfo, notificationTitle);

            return Ok(model);
        }

        /// <summary>
        /// Updates a Picture entity in the database.
        /// Also updates the corresponding TimeLineItem entity.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to update.</param>
        /// <param name="value">Picture object with the updated properties.</param>
        /// <returns>The updated Picture object.</returns>
        // PUT api/pictures/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Picture value)
        {
            Picture picture = await picturesService.GetPicture(id);

            // Todo: more validation of the values
            if (picture == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            picture = await picturesService.UpdatePicture(value);

            await picturesService.SetPictureInCache(picture.PictureId);
            await commentsService.SetCommentsList(picture.CommentThreadNumber);


            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(picture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
            if (timeLineItem == null) return Ok(picture);

            

            timeLineItem.CopyPicturePropertiesForUpdate(picture);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            return Ok(picture);
        }

        /// <summary>
        /// Deletes a Picture entity from the database.
        /// Also deletes the corresponding CommentThread entity, associated Comments and TimeLineItem entity.
        /// Then notifies admins for the Progeny.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to delete.</param>
        /// <returns>NoContentResult</returns>
        // DELETE api/progeny/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Picture picture = await picturesService.GetPicture(id);
            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            List<Comment> comments = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            if (comments.Count != 0)
            {
                foreach (Comment deletedComment in comments)
                {
                    await commentsService.DeleteComment(deletedComment);
                }
            }

            CommentThread cmntThread = await commentsService.GetCommentThread(picture.CommentThreadNumber);
            if (cmntThread != null)
            {
                await commentsService.RemoveCommentsList(picture.CommentThreadNumber);
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(picture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
            if (timeLineItem != null)
            {
                await timelineService.DeleteTimeLineItem(timeLineItem);
            }
            
            await picturesService.DeletePicture(picture);
            await picturesService.RemovePictureFromCache(picture.PictureId, picture.ProgenyId);

            if (timeLineItem == null) return NoContent();

            Progeny progeny = await progenyService.GetProgeny(picture.ProgenyId);
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "Photo deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FirstName + " " + userInfo.MiddleName + " " + userInfo.LastName + " deleted a photo for " + progeny.NickName;

            picture.AccessLevel = timeLineItem.AccessLevel = 0;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendPictureNotification(picture, userInfo, notificationTitle);

            return NoContent();

        }

        /// <summary>
        /// Gets a random Picture entity for a given ProgenyId and AccessLevel.
        /// Does not include comments.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get a Picture for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>The Picture object.</returns>
        // GET api/pictures/random/[Progeny id]?accessLevel=5
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> Random(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Count != 0)
            {
                Random r = new();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                if (!picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) && !picture.PictureLink.Contains('.'))
                {
                    picture = await picturesService.GetPicture(picture.PictureId);
                }

                return Ok(picture);
            }

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();
            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets a random Picture entity for a given ProgenyId and AccessLevel.
        /// Does not include comments.
        /// For use in mobile clients.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get a picture for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>The Picture object.</returns>
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> RandomMobile(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
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

            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets a Picture entity for a given PictureId.
        /// Does not include comments.
        /// For use in mobile clients.
        /// </summary>
        /// <param name="id">The PictureId of the Picture entity to get.</param>
        /// <returns>The Picture object.</returns>
        // GET api/pictures/5
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await picturesService.GetPicture(id);
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                if (result.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return Ok(result);

                result.PictureLink = imageStore.UriFor(result.PictureLink);
                result.PictureLink1200 = imageStore.UriFor(result.PictureLink1200);
                result.PictureLink600 = imageStore.UriFor(result.PictureLink600);
                return Ok(result);
            }


            Picture tempPicture = new();
            tempPicture.ApplyPlaceholderProperties();

            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets PicturePageViewModel for a photo gallery page for a given ProgenyId, AccessLevel, and Tag.
        /// For use in mobile clients.
        /// </summary>
        /// <param name="pageSize">Number of Picture per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to view photos for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <param name="tagFilter">Only include Picture items that contain the tag string, unless it is empty then include all Picture items.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1= newest first.</param>
        /// <returns>PicturePageViewModel</returns>
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
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

            List<Picture> allItems = await picturesService.GetPicturesList(progenyId);
            List<string> tagsList = [];
            foreach (Picture pic in allItems)
            {
                if (string.IsNullOrEmpty(pic.Tags)) continue;

                List<string> pictureTagsList = [.. pic.Tags.Split(',')];
                foreach (string tagString in pictureTagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        tagsList.Add(trimmedTag);
                    }
                }
            }
            if (!string.IsNullOrEmpty(tagFilter))
            {

                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }
            else
            {
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
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
                pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                pic.PictureLink = imageStore.UriFor(pic.PictureLink);
                pic.PictureLink1200 = imageStore.UriFor(pic.PictureLink1200);
                pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
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

        /// <summary>
        /// Gets a PictureViewModel for a Picture entity with the provided PictureId.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get the PictureViewModel for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny the Picture belongs to.</param>
        /// <param name="sortBy">Sort order for determining next and previous picture, 0 = oldest first, 1 = newest first.</param>
        /// <returns>PictureViewModel</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed for mobile clients.")]
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel = 5, [FromQuery] int sortBy = 1)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null || picture.AccessLevel < userAccess.AccessLevel)
            {
                return Unauthorized();
            }

            PictureViewModel model = new();
            model.SetPicturePropertiesFromPictureItem(picture);

            model.PictureLink = imageStore.UriFor(model.PictureLink);
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime)];
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
                    foreach (string tagString in pvmTags)
                    {
                        string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                        if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                        {
                            tagsList.Add(trimmedTag);
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

            model.SetTagsList(tagsList);

            return Ok(model);

        }

        /// <summary>
        /// Gets a PictureViewModel for a Picture entity with the provided PictureId.
        /// For use in Maui clients.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get the PictureViewModel for.</param>
        /// <returns>PictureViewModel</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> PictureViewModelMaui(int id)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null)
            {
                return Unauthorized();
            }

            PictureViewModel model = new();
            model.SetPicturePropertiesFromPictureItem(picture);
            model.PictureLink = imageStore.UriFor(model.PictureLink);
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
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
                    if (string.IsNullOrEmpty(pictureItem.Tags)) continue;

                    List<string> pictureTagsList = [.. pictureItem.Tags.Split(',')];
                    foreach (string tagString in pictureTagsList)
                    {
                        string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                        if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                        {
                            tagsList.Add(trimmedTag);
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

                (model.NextPicture, model.PrevPicture) = (model.PrevPicture, model.NextPicture);
            }

            model.SetTagsList(tagsList);

            return Ok(model);

        }

        /// <summary>
        /// Adds an image file to the Pictures Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to add.</param>
        /// <returns>String with the filename (PictureLink) of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadPicture([FromForm] IFormFile file)
        {
            string pictureLink;
            await using (Stream stream = file.OpenReadStream())
            {
                string fileFormat = Path.GetExtension(file.FileName);
                pictureLink = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Adds an image file to the Progeny Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to add.</param>
        /// <returns>String with the filename of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProgenyPicture([FromForm] IFormFile file)
        {
            string pictureLink = await picturesService.ProcessProgenyPicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Adds an image file to the User Profile Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to add.</param>
        /// <returns>String with the filename of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
        {
            string pictureLink = await picturesService.ProcessProfilePicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Adds an image file to the Friend Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to upload.</param>
        /// <returns>String with the filename of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadFriendPicture([FromForm] IFormFile file)
        {
            string pictureLink = await picturesService.ProcessFriendPicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Adds an image file to the Contact Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to add.</param>
        /// <returns>String with the filename of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadContactPicture([FromForm] IFormFile file)
        {
            string pictureLink = await picturesService.ProcessContactPicture(file);

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Adds an image file to the Note Blob storage and returns the filename for the image.
        /// </summary>
        /// <param name="file">IFormFile content with the file to add.</param>
        /// <returns>String with the filename of the file added.</returns>
        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadNoteImage([FromForm] IFormFile file)
        {
            string pictureLink;
            await using (Stream stream = file.OpenReadStream())
            {
                string fileFormat = Path.GetExtension(file.FileName);
                pictureLink = await imageStore.SaveImage(stream, BlobContainers.Notes, fileFormat);
                pictureLink = imageStore.UriFor(pictureLink, BlobContainers.Notes);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        /// <summary>
        /// Get a URL for a Picture entity with the provided PictureId.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get an URL for.</param>
        /// <returns>String with the URL.</returns>
        [Route("[action]/{id}")]
        [HttpGet]
        public IActionResult GetProfilePicture(string id)
        {
            string result = imageStore.UriFor(id, "profiles");

            if (string.IsNullOrEmpty(result))
            {
                result = Constants.ProfilePictureUrl;
            }

            return Ok(result);
        }

        [Route("[action]/{progenyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetPictureLocations(int progenyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Location> pictureLocation = await picturesService.GetPicturesLocations(progenyId);
            return Ok(pictureLocation);
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> GetPicturesNearLocation([FromBody] Location location)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(location.ProgenyId, userEmail);

            if (userAccess == null && location.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> pictures = await picturesService.GetPicturesNearLocation(location);
            return Ok(pictures);
        }

        /// <summary>
        /// Gets a list of string with all locations from Picture and Video entities for a given ProgenyId and AccessLevel.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to locations for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>List of string with location names.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> allPictures = await picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();

            List<string> autoSuggestList = [];

            foreach (Picture picture in allPictures)
            {
                if (string.IsNullOrEmpty(picture.Location)) continue;

                if (!autoSuggestList.Contains(picture.Location))
                {
                    autoSuggestList.Add(picture.Location);
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Location)) continue;

                if (!autoSuggestList.Contains(video.Location))
                {
                    autoSuggestList.Add(video.Location);
                }
            }

            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Gets a list of string with all tags from Picture and Video entities for a given ProgenyId and AccessLevel.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get tags for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>List of strings for each tag.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }


            List<Picture> allPictures = await picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];

            foreach (Picture picture in allPictures)
            {
                if (string.IsNullOrEmpty(picture.Tags)) continue;

                List<string> tagsList = [.. picture.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        autoSuggestList.Add(trimmedTag);
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();

            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Tags)) continue;

                List<string> tagsList = [.. video.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        autoSuggestList.Add(trimmedTag);
                    }
                }
            }
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }


        /// <summary>
        /// If a Picture has a URL in the PictureLink property, this will download the image file and save it to the Blob storage.
        /// Then updates the Picture entity with the new filename.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to update.</param>
        /// <returns>The updated Picture object</returns>
        // Download pictures to StorageBlob from Url
        [HttpGet]
        [Route("[action]/{pictureId:int}")]
        public async Task<IActionResult> DownloadPicture(int pictureId)
        {
            Picture picture = await picturesService.GetPicture(pictureId);
            if (picture == null || !picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            await using (Stream stream = await GetStreamFromUrl(picture.PictureLink))
            {
                string fileFormat = "";
                
                if (picture.PictureLink.ToLower().EndsWith(".jpg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink.ToLower().EndsWith(".png"))
                {
                    fileFormat = ".png";
                }

                if (picture.PictureLink.ToLower().EndsWith(".gif"))
                {
                    fileFormat = ".gif";
                }

                if (picture.PictureLink.ToLower().EndsWith(".jpeg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink.ToLower().EndsWith(".bmp"))
                {
                    fileFormat = ".bmp";
                }

                if (picture.PictureLink.ToLower().EndsWith(".tif"))
                {
                    fileFormat = ".tif";
                }

                if (picture.PictureLink.ToLower().EndsWith(".webp"))
                {
                    fileFormat = ".webp";
                }

                picture.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
            }

            await using (Stream stream = await GetStreamFromUrl(picture.PictureLink600))
            {
                string fileFormat = "";
                if (picture.PictureLink600.ToLower().EndsWith(".jpg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".png"))
                {
                    fileFormat = ".png";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".gif"))
                {
                    fileFormat = ".gif";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".jpeg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".bmp"))
                {
                    fileFormat = ".bmp";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".tif"))
                {
                    fileFormat = ".tif";
                }

                if (picture.PictureLink600.ToLower().EndsWith(".webp"))
                {
                    fileFormat = ".webp";
                }

                picture.PictureLink600 = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
            }

            await using (Stream stream = await GetStreamFromUrl(picture.PictureLink1200))
            {
                string fileFormat = "";
                if (picture.PictureLink1200.ToLower().EndsWith(".jpg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".png"))
                {
                    fileFormat = ".png";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".gif"))
                {
                    fileFormat = ".gif";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".jpeg"))
                {
                    fileFormat = ".jpg";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".bmp"))
                {
                    fileFormat = ".bmp";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".tif"))
                {
                    fileFormat = ".tif";
                }

                if (picture.PictureLink1200.ToLower().EndsWith(".webp"))
                {
                    fileFormat = ".webp";
                }

                picture.PictureLink1200 = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
            }

            picture = await picturesService.UpdatePicture(picture);
            return Ok(picture);

        }

        /// <summary>
        /// Gets a Stream for a file from a URL.
        /// </summary>
        /// <param name="url">The URL to read the file from.</param>
        /// <returns>Stream with the file content.</returns>
        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
