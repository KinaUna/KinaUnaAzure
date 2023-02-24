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

namespace KinaUnaWeb.Controllers
{
    public class VideosController : Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IEmailSender _emailSender;
        private readonly IViewModelSetupService _viewModelSetupService;
        
        public VideosController(IMediaHttpClient mediaHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient,
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
            if (id < 1)
            {
                id = 1;
            }

            // VideoPageViewModel is used by KinaUna Xamarin and ProgenyApi, so it should not be changed in this project, instead using a different view model and copying the properties.
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VideoListViewModel model = new VideoListViewModel(baseModel);
            
            VideoPageViewModel pageViewModel = await _mediaHttpClient.GetVideoPage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);
            model.SortBy = sortBy;
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Video(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
        {
            Video video = await _mediaHttpClient.GetVideo(id, Constants.DefaultTimezone);
            if (video == null)
            {
                return RedirectToAction("Index");
            }

            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoItemViewModel model = new VideoItemViewModel(baseModel);
            VideoViewModel videoViewModel = await _mediaHttpClient.GetVideoViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone);
            
            model.SetPropertiesFromVideoViewModel(videoViewModel);
            
            if (model.CommentsCount > 0)
            {
                foreach (Comment cmnt in model.CommentsList)
                {
                    UserInfo commentAuthor = await _userInfosHttpClient.GetUserInfoByUserId(cmnt.Author);
                    string commentAuthorProfilePicture = commentAuthor?.ProfilePicture ?? "";
                    commentAuthorProfilePicture = _imageStore.UriFor(commentAuthorProfilePicture, "profiles");
                    cmnt.AuthorImage = commentAuthorProfilePicture;
                    cmnt.DisplayName = commentAuthor.FullName();
                }
            }
            if (model.IsCurrentUserProgenyAdmin)
            {
                model.ProgenyLocations = new List<Location>();
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

        [AllowAnonymous]
        public IActionResult Youtube(string link)
        {
            return PartialView("Youtube", link);
        }

        public async Task<IActionResult> AddVideo()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            UploadVideoViewModel model = new UploadVideoViewModel(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Videos",
                    action = "Index"
                });
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.Video.Owners = model.CurrentUser.UserEmail;
                model.Video.Author = model.CurrentUser.UserId;
                model.Video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVideo(UploadVideoViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Videos",
                    action = "Index"
                });
            }

            Video videoToAdd = model.CreateVideo(true);

            _ = await _mediaHttpClient.AddVideo(videoToAdd);
            
            model.SetAccessLevelList();

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(VideoItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Videos",
                    action = "Video",
                    id = model.Video.VideoId,
                    childId = model.Video.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            Video videoToUpdate = await _mediaHttpClient.GetVideo(model.Video.VideoId, model.CurrentUser.Timezone);
            videoToUpdate.CopyPropertiesForUpdate(model.Video, true);
            
            if (model.Video.VideoTime != null)
            {
                videoToUpdate.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.Video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            
            _ = await _mediaHttpClient.UpdateVideo(videoToUpdate);
            
            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.Video.VideoId, childId = model.Video.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            Video video = await _mediaHttpClient.GetVideo(videoId, "");
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoItemViewModel model = new VideoItemViewModel(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Videos",
                    action = "Video",
                    id = model.Video.VideoId,
                    childId = model.Video.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            model.SetPropertiesFromVideoItem(video);
            
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(VideoItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.IsCurrentUserProgenyAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Videos",
                    action = "Video",
                    id = model.Video.VideoId,
                    childId = model.Video.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            if (model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                _ = await _mediaHttpClient.DeleteVideo(model.Video.VideoId);
            }
            
            // Todo: else, error, show info
            // Todo: show confirmation info, instead of gallery page.
            
            return RedirectToAction("Index", "Videos");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideoComment(CommentViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            
            Comment comment = model.CreateComment((int)KinaUnaTypes.TimeLineType.Video);
            
            bool commentAdded = await _mediaHttpClient.AddVideoComment(comment);

            if (commentAdded)
            {
                if (model.CurrentProgeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Videos/Video/" + model.ItemId + "?childId=" + model.CurrentProgenyId;
                    List<string> emails = model.CurrentProgeny.Admins.Split(",").ToList();
                    foreach (string toMail in emails)
                    {
                        await _emailSender.SendEmailAsync(toMail, "New Comment on " + model.CurrentProgeny.NickName + "'s Video",
                           "A comment was added to " + model.CurrentProgeny.NickName + "'s video by " + comment.DisplayName + ":<br/><br/>" + comment.CommentText + "<br/><br/>Video Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                    
                }
            }

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.ItemId, childId = model.CurrentProgenyId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoComment(int commentThreadNumber, int commentId, int videoId, int progenyId)
        {
            await _mediaHttpClient.DeleteVideoComment(commentId);

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = videoId, childId = progenyId });
        }
    }
}