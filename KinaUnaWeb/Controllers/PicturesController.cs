using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace KinaUnaWeb.Controllers
{
    public class PicturesController : Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IEmailSender _emailSender;
        private readonly IViewModelSetupService _viewModelSetupService;

        public PicturesController(IMediaHttpClient mediaHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient,
            ILocationsHttpClient locationsHttpClient, IEmailSender emailSender, IViewModelSetupService viewModelSetupService)
        {
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _emailSender = emailSender;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 8, int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            PicturesListViewModel model = new PicturesListViewModel(baseModel);

            model.PageSize = pageSize;
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;

            // PicturePageViewModel is used by KinaUna Xamarin and ProgenyApi, so it should not be changed in this project, instead using a different view model and copying the properties.
            PicturePageViewModel pageViewModel = await _mediaHttpClient.GetPicturePage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);

            //model.SortBy = sortBy;
            //model.PageSize = pageSize;
            foreach (Picture pic in model.PicturesList)
            {
                pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Picture(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
        {
            Picture picture = await _mediaHttpClient.GetPicture(id, Constants.DefaultTimezone);
            if (picture == null)
            {
                return RedirectToAction("Index");
            }

            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            PictureItemViewModel model = new PictureItemViewModel(baseModel);
            PictureViewModel pictureViewModel = await _mediaHttpClient.GetPictureViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone, tagFilter);

            model.SetPropertiesFromPictureViewModel(pictureViewModel);
            model.Picture.PictureLink = _imageStore.UriFor(model.Picture.PictureLink);
            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            
            if (model.CommentsCount > 0)
            {
                foreach(Comment comment in model.CommentsList)
                {
                    UserInfo commentAuthor = await _userInfosHttpClient.GetUserInfoByUserId(comment.Author);
                    string commentAuthorProfilePicture = commentAuthor?.ProfilePicture ?? "";
                    commentAuthorProfilePicture = _imageStore.UriFor(commentAuthorProfilePicture, "profiles");
                    comment.AuthorImage = commentAuthorProfilePicture;
                    comment.DisplayName = commentAuthor.FullName();
                }
            }
            if (model.IsCurrentUserProgenyAdmin)
            {
                model.ProgenyLocations = await _locationsHttpClient.GetProgenyLocations(model.CurrentProgenyId, model.CurrentAccessLevel);
                model.LocationsList = new List<SelectListItem>();
                if (model.ProgenyLocations.Any())
                {
                    foreach (Location loc in model.ProgenyLocations)
                    {
                        SelectListItem selectListItem = new SelectListItem();
                        selectListItem.Text = loc.Name;
                        selectListItem.Value = loc.LocationId.ToString();
                        model.LocationsList.Add(selectListItem);
                    }
                }
            }

            model.SetAccessLevelList();

            return View(model);
        }

        public async Task<IActionResult> AddPicture()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            UploadPictureViewModel model = new UploadPictureViewModel(baseModel);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPictures(UploadPictureViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "Index"
                });
            }

            List<Picture> pictureList = new List<Picture>();
            UploadPictureViewModel result = new UploadPictureViewModel();
            result.LanguageId = model.LanguageId;
            result.FileLinks = new List<string>();
            result.FileNames = new List<string>();
            if (model.Files.Any())
            {
                foreach (IFormFile formFile in model.Files)
                {
                    Picture picture = new Picture();
                    picture.ProgenyId = model.Picture.ProgenyId;
                    picture.AccessLevel = model.Picture.AccessLevel;
                    picture.Author = model.CurrentUser.UserId;
                    picture.Owners = model.CurrentUser.UserEmail;
                    picture.TimeZone = model.CurrentUser.Timezone;

                    await using (Stream stream = formFile.OpenReadStream())
                    {
                        picture.PictureLink = await _imageStore.SaveImage(stream);
                    }

                    Picture newPicture = await _mediaHttpClient.AddPicture(picture);
                    
                    pictureList.Add(newPicture);
                }
            }

            if (pictureList.Any())
            {
                foreach (Picture pic in pictureList)
                {
                    result.FileLinks.Add(_imageStore.UriFor(pic.PictureLink600));
                    result.FileNames.Add(_imageStore.UriFor(pic.PictureLink600));
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
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

            Picture pictureToUpdate = await _mediaHttpClient.GetPicture(model.Picture.PictureId, model.CurrentUser.Timezone);
            pictureToUpdate.CopyPropertiesForUpdate(model.Picture);
            
            if (model.Picture.PictureTime != null)
            {
                pictureToUpdate.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.Picture.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            _ = await _mediaHttpClient.UpdatePicture(pictureToUpdate);
            
            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.Picture.PictureId, childId = model.Picture.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletePicture(int pictureId)
        {
            Picture picture = await _mediaHttpClient.GetPicture(pictureId, "");
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), picture.ProgenyId);
            PictureItemViewModel model = new PictureItemViewModel(baseModel);

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
            model.Picture.PictureLink600 = _imageStore.UriFor(model.Picture.PictureLink600);
            
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePicture(PictureItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Picture.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                _ = await _mediaHttpClient.DeletePicture(model.Picture.PictureId);

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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);

            Comment comment = model.CreateComment((int)KinaUnaTypes.TimeLineType.Photo);
            
            bool commentAdded = await _mediaHttpClient.AddPictureComment(comment);

            if (commentAdded)
            {
                if (model.CurrentProgeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Pictures/Picture/" + model.ItemId + "?childId=" + model.CurrentProgenyId;
                    List<string> emails = model.CurrentProgeny.Admins.Split(",").ToList();

                    foreach (string toMail in emails)
                    {
                        await _emailSender.SendEmailAsync(toMail, "New Comment on " + model.CurrentProgeny.NickName + "'s Picture",
                           "A comment was added to " + model.CurrentProgeny.NickName + "'s picture by " + comment.DisplayName + ":<br/><br/>" + comment.CommentText + "<br/><br/>Picture Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                }
            }

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.ItemId, childId = model.CurrentProgenyId, sortBy = model.SortBy });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePictureComment(int commentThreadNumber, int commentId, int pictureId, int progenyId)
        {
            await _mediaHttpClient.DeletePictureComment(commentId);

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = pictureId, childId = progenyId });
        }
    }
}