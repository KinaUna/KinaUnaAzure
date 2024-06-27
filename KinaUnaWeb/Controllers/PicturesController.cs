using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using KinaUnaWeb.Models.TypeScriptModels.Pictures;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    public class PicturesController(
        IMediaHttpClient mediaHttpClient,
        ImageStore imageStore,
        IUserInfosHttpClient userInfosHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IEmailSender emailSender,
        IViewModelSetupService viewModelSetupService,
        IConfiguration configuration)
        : Controller
    {
        private readonly string _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 10, int childId = 0, int sortBy = 2, string tagFilter = "", int year = 0, int month = 0, int day = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            PicturesListViewModel model = new(baseModel)
            {
                PageSize = pageSize,
                SortBy = sortBy,
                TagFilter = tagFilter,
                Year = year,
                Month = month,
                Day = day,
                PicturePageParameters = new()
                {
                    ProgenyId = baseModel.CurrentProgenyId,
                    CurrentPageNumber = id,
                    ItemsPerPage = pageSize,
                    Sort = sortBy,
                    TagFilter = tagFilter,
                    Year = year,
                    Month = month,
                    Day = day
                }
            };
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Picture(int id, string tagFilter = "", int sortBy = 1, bool partialView = false)
        {
            Picture picture = await mediaHttpClient.GetPicture(id, Constants.DefaultTimezone);
            if (picture == null)
            {
                return RedirectToAction("Index");
            }

            if (picture.PictureId == 0)
            {
                picture.PictureId = id;
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            PictureItemViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey,
                PartialView = partialView
            };
            PictureViewModel pictureViewModel = await mediaHttpClient.GetPictureViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone, tagFilter);

            model.SetPropertiesFromPictureViewModel(pictureViewModel);
            model.Picture.PictureLink = model.Picture.GetPictureUrl(1200);

            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            
            if (model.CommentsCount > 0)
            {
                foreach(Comment comment in model.CommentsList)
                {
                    UserInfo commentAuthor = await userInfosHttpClient.GetUserInfoByUserId(comment.Author);
                    if (commentAuthor == null) continue;
                    if (commentAuthor.ProfilePicture != null)
                    {
                        comment.AuthorImage = commentAuthor.GetProfilePictureUrl();
                    }
                        
                    comment.DisplayName = commentAuthor.FullName();

                }
            }
            if (model.IsCurrentUserProgenyAdmin)
            {
                model.ProgenyLocations = await locationsHttpClient.GetProgenyLocations(model.CurrentProgenyId, model.CurrentAccessLevel);
                model.LocationsList = [];
                if (model.ProgenyLocations.Count != 0)
                {
                    foreach (Location loc in model.ProgenyLocations)
                    {
                        SelectListItem selectListItem = new()
                        {
                            Text = loc.Name,
                            Value = loc.LocationId.ToString()
                        };
                        model.LocationsList.Add(selectListItem);
                    }
                }
            }

            model.SetAccessLevelList();

            if (partialView)
            {
                return PartialView("_PictureDetailsPartial", model);
            }

            return View(model);
        }

        public async Task<FileContentResult> OriginalPicture(int id)
        {
            Picture picture = await mediaHttpClient.GetPicture(id, Constants.DefaultTimezone);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            if (baseModel.CurrentAccessLevel > picture.AccessLevel || picture.PictureId == 0)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/png");
            }

            MemoryStream fileContent = await imageStore.GetStream(picture.PictureLink);
            byte[] fileContentBytes = fileContent.ToArray();
            
            return new FileContentResult(fileContentBytes, picture.GetPictureFileContentType());
        }

        [AllowAnonymous]
        public async Task<FileContentResult> File([FromQuery] int id, [FromQuery] int size)
        {
            Picture picture = await mediaHttpClient.GetPicture(id, Constants.DefaultTimezone);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            if (baseModel.CurrentAccessLevel > picture.AccessLevel || picture.PictureId == 0)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png");
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

        public async Task<IActionResult> AddPicture()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            UploadPictureViewModel model = new(baseModel);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPictures(UploadPictureViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "Index"
                });
            }

            List<Picture> pictureList = [];
            UploadPictureViewModel result = new()
            {
                LanguageId = model.LanguageId,
                FileLinks = [],
                FileNames = []
            };
            if (model.Files.Count != 0)
            {
                foreach (IFormFile formFile in model.Files)
                {
                    Picture picture = new()
                    {
                        ProgenyId = model.Picture.ProgenyId,
                        AccessLevel = model.Picture.AccessLevel,
                        Author = model.CurrentUser.UserId,
                        Owners = model.CurrentUser.UserEmail,
                        TimeZone = model.CurrentUser.Timezone,
                        Location = model.Picture.Location,
                        Tags = model.Picture.Tags
                    };

                    await using (Stream stream = formFile.OpenReadStream())
                    {
                        string fileFormat = Path.GetExtension(formFile.FileName);
                        picture.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
                    }

                    Picture newPicture = await mediaHttpClient.AddPicture(picture);
                    
                    pictureList.Add(newPicture);
                }
            }

            if (pictureList.Count != 0)
            {
                foreach (Picture pic in pictureList)
                {
                    result.FileLinks.Add(pic.GetPictureUrl(600));
                    result.FileNames.Add(pic.PictureId.ToString());
                }
            }

            model.SetAccessLevelList();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePicture(UploadPictureViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "Index"
                });
            }

            List<Picture> pictureList = [];
            
            if (model.Files.Count != 0)
            {
                foreach (IFormFile formFile in model.Files)
                {
                    Picture picture = new()
                    {
                        ProgenyId = model.Picture.ProgenyId,
                        AccessLevel = model.Picture.AccessLevel,
                        Author = model.CurrentUser.UserId,
                        Owners = model.CurrentUser.UserEmail,
                        TimeZone = model.CurrentUser.Timezone,
                        Location = model.Picture.Location,
                        Tags = model.Picture.Tags
                    };

                    await using (Stream stream = formFile.OpenReadStream())
                    {
                        string fileFormat = Path.GetExtension(formFile.FileName);
                        picture.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Pictures, fileFormat);
                        
                    }

                    Picture newPicture = await mediaHttpClient.AddPicture(picture);

                    pictureList.Add(newPicture);
                }
            }

            Picture savedPicture = pictureList.FirstOrDefault();
            if (savedPicture == null) return Json(null);

            savedPicture.PictureLink600 = savedPicture.GetPictureUrl(600);

            return Json(savedPicture);

        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPicture(PictureItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Pictures",
                    action = "Picture",
                    id = model.Picture.PictureId,
                    childId = model.Picture.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            Picture pictureToUpdate = await mediaHttpClient.GetPicture(model.Picture.PictureId, model.CurrentUser.Timezone);
            pictureToUpdate.CopyPropertiesForUserUpdate(model.Picture);
            
            if (model.Picture.PictureTime != null)
            {
                pictureToUpdate.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.Picture.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            _ = await mediaHttpClient.UpdatePicture(pictureToUpdate);

            if (model.PartialView)
            {
                return Json(model);
            }
            
            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.Picture.PictureId, childId = model.Picture.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletePicture(int pictureId)
        {
            Picture picture = await mediaHttpClient.GetPicture(pictureId, "");
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            PictureItemViewModel model = new(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Pictures",
                    action = "Picture",
                    id = model.Picture.PictureId,
                    childId = model.Picture.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            model.SetPropertiesFromPictureItem(picture);
            model.Picture.PictureLink600 = model.Picture.GetPictureUrl(600);
            
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePicture(PictureItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                _ = await mediaHttpClient.DeletePicture(model.Picture.PictureId);

            }

            // Todo: else, error, show info

            // Todo: show confirmation info, instead of gallery page.

            return RedirectToAction("Index", "Pictures");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPictureComment(CommentViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            Comment comment = model.CreateComment((int)KinaUnaTypes.TimeLineType.Photo);
            
            bool commentAdded = await mediaHttpClient.AddPictureComment(comment);

            if (!commentAdded) return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.ItemId, childId = model.CurrentProgenyId, sortBy = model.SortBy });

            if (model.CurrentProgeny == null) return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.ItemId, childId = model.CurrentProgenyId, sortBy = model.SortBy });

            string imgLink = Constants.WebAppUrl + "/Pictures/Picture/" + model.ItemId + "?childId=" + model.CurrentProgenyId;
            List<string> emails = [.. model.CurrentProgeny.Admins.Split(",")];

            foreach (string toMail in emails)
            {
                await emailSender.SendEmailAsync(toMail, "New Comment on " + model.CurrentProgeny.NickName + "'s Picture",
                    "A comment was added to " + model.CurrentProgeny.NickName + "'s picture by " + comment.DisplayName + ":<br/><br/>" + comment.CommentText + "<br/><br/>Picture Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
            }

            if (model.PartialView)
            {
                return Json(model);
            }

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.ItemId, childId = model.CurrentProgenyId, sortBy = model.SortBy });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePictureComment(int commentId, int pictureId, int progenyId)
        {
            await mediaHttpClient.DeletePictureComment(commentId);

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = pictureId, childId = progenyId });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetPictureList([FromBody] PicturesPageParameters parameters)
        {
            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            if (parameters.ItemsPerPage < 1)
            {
                parameters.ItemsPerPage = 10;
            }

            if (parameters.Sort > 1)
            {
                parameters.Sort = 1;
            }


            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), parameters.ProgenyId);

            PicturesList picturesList = new()
            {
                PictureItems = await mediaHttpClient.GetProgenyPictureList(baseModel.CurrentProgenyId, baseModel.CurrentAccessLevel)
            };

            picturesList.PictureItems = picturesList.PictureItems.OrderBy(p => p.PictureTime).ToList();

            if (!string.IsNullOrEmpty(parameters.TagFilter))
            {
                picturesList.PictureItems = [.. picturesList.PictureItems.Where(p => p.Tags != null && p.Tags.Contains(parameters.TagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }

            int pictureCounter = 1;
            

            List<string> tagsList = [];
            foreach (Picture picture in picturesList.PictureItems)
            {
                picture.PictureNumber = pictureCounter;
                
                pictureCounter++;
                if (string.IsNullOrEmpty(picture.Tags)) continue;

                List<string> picturePageViewModelTagsList = [.. picture.Tags.Split(',')];
                foreach (string tagString in picturePageViewModelTagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        tagsList.Add(trimmedTag);
                    }
                }
            }
            picturesList.TagsList = tagsList;

            DateTime currentDateTime = DateTime.UtcNow;
            DateTime firstItemTime = picturesList.PictureItems.Where(p => p.PictureTime.HasValue).Min(t => t.PictureTime)?? currentDateTime;
            
            if (parameters.Year == 0)
            {
                if (parameters.Sort == 1)
                {
                    parameters.Year = currentDateTime.Year;
                    parameters.Month = currentDateTime.Month;
                    parameters.Day = currentDateTime.Day;
                }
                else
                {
                    parameters.Year = firstItemTime.Year;
                    parameters.Month = 1;
                    parameters.Day = 1;
                }
            }

            DateTime startDate = new(parameters.Year, parameters.Month, parameters.Day, 23, 59, 59);
            startDate = TimeZoneInfo.ConvertTimeToUtc(startDate, TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
            if (parameters.Sort == 1)
            {

                picturesList.PictureItems = picturesList.PictureItems.Where(t => t.PictureTime <= startDate).OrderByDescending(p => p.PictureTime).ToList();
            }
            else
            {
                startDate = new DateTime(parameters.Year, parameters.Month, parameters.Day, 0, 0, 0);
                startDate = TimeZoneInfo.ConvertTimeToUtc(startDate, TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
                picturesList.PictureItems = picturesList.PictureItems.Where(t => t.PictureTime >= startDate).OrderBy(p => p.PictureTime).ToList();
            }

            firstItemTime = TimeZoneInfo.ConvertTimeFromUtc(firstItemTime, TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
            picturesList.FirstItemYear = firstItemTime.Year;

            int skip = (parameters.CurrentPageNumber - 1) * parameters.ItemsPerPage;
            picturesList.AllItemsCount = picturesList.PictureItems.Count;
            picturesList.RemainingItemsCount = picturesList.PictureItems.Count - skip - parameters.ItemsPerPage;
            picturesList.PictureItems = picturesList.PictureItems.Skip(skip).Take(parameters.ItemsPerPage).ToList();
            picturesList.TotalPages = (int)Math.Ceiling((double)picturesList.AllItemsCount / parameters.ItemsPerPage);
            picturesList.CurrentPageNumber = parameters.CurrentPageNumber;
            
            foreach (Picture pic in picturesList.PictureItems)
            {
                if (pic.PictureTime.HasValue)
                {
                    pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
                }
            }
            return Json(picturesList);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetPictureElement([FromBody] PictureViewModel pictureViewModel)
        {
            Picture picture = await mediaHttpClient.GetPicture(pictureViewModel.PictureId, Constants.DefaultTimezone);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            PictureViewModel pictureViewModelData = await mediaHttpClient.GetPictureElement(pictureViewModel.PictureId);
            PictureItemViewModel model = new(baseModel);
           
            model.SetPropertiesFromPictureViewModel(pictureViewModelData);
            model.Picture.PictureLink = model.Picture.GetPictureUrl(600);
            model.PictureNumber = pictureViewModel.PictureNumber;
            model.Picture.PictureNumber = pictureViewModel.PictureNumber;
            return PartialView("_GetPictureElementPartial", model);
        }
    }
}