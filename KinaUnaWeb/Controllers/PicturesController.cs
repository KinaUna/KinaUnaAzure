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
        public async Task<IActionResult> Index(int id = 1, int pageSize = 16, int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            PicturesListViewModel model = new(baseModel)
            {
                PageSize = pageSize,
                SortBy = sortBy,
                TagFilter = tagFilter
            };

            // PicturePageViewModel is used by KinaUna Xamarin and ProgenyApi, so it should not be changed in this project, instead using a different view model and copying the properties.
            PicturePageViewModel pageViewModel = await mediaHttpClient.GetPicturePage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);

            //model.SortBy = sortBy;
            //model.PageSize = pageSize;
            foreach (Picture pic in model.PicturesList)
            {
                pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Picture(int id, string tagFilter = "", int sortBy = 1)
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
                HereMapsApiKey = _hereMapsApiKey
            };
            PictureViewModel pictureViewModel = await mediaHttpClient.GetPictureViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone, tagFilter);

            model.SetPropertiesFromPictureViewModel(pictureViewModel);
            model.Picture.PictureLink = imageStore.UriFor(model.Picture.PictureLink);

            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            
            if (model.CommentsCount > 0)
            {
                foreach(Comment comment in model.CommentsList)
                {
                    UserInfo commentAuthor = await userInfosHttpClient.GetUserInfoByUserId(comment.Author);
                    string commentAuthorProfilePicture = commentAuthor?.ProfilePicture ?? "";
                    commentAuthorProfilePicture = imageStore.UriFor(commentAuthorProfilePicture, "profiles");
                    comment.AuthorImage = commentAuthorProfilePicture;
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

            return View(model);
        }

        public async Task<FileContentResult> OriginalPicture(int id)
        {
            Picture picture = await mediaHttpClient.GetPicture(id, Constants.DefaultTimezone);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            if (baseModel.CurrentAccessLevel > picture.AccessLevel)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/png");
            }

            MemoryStream fileContent = await imageStore.GetStream(picture.PictureLink);
            byte[] fileContentBytes = fileContent.ToArray();

            if (picture.PictureLink.Contains(".png"))
                return new FileContentResult(fileContentBytes, "image/png");
            if (picture.PictureLink.Contains(".gif"))
                return new FileContentResult(fileContentBytes, "image/gif");
            if (picture.PictureLink.Contains(".bmp"))
                return new FileContentResult(fileContentBytes, "image/bmp");
            if (picture.PictureLink.Contains(".tiff"))
                return new FileContentResult(fileContentBytes, "image/tiff");
            if (picture.PictureLink.Contains(".webp"))
                return new FileContentResult(fileContentBytes, "image/webp");
            
            return new FileContentResult(fileContentBytes, "image/jpeg");
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
                    result.FileLinks.Add(imageStore.UriFor(pic.PictureLink600));
                    result.FileNames.Add(imageStore.UriFor(pic.PictureLink600));
                }
            }

            model.SetAccessLevelList();

            return View(result);
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
            model.Picture.PictureLink600 = imageStore.UriFor(model.Picture.PictureLink600);
            
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
    }
}