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
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    public class VideosController(
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
            if (id < 1)
            {
                id = 1;
            }

            // VideoPageViewModel is used by KinaUna Xamarin and ProgenyApi, so it should not be changed in this project, instead using a different view model and copying the properties.
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VideoListViewModel model = new(baseModel)
            {
                PageSize = pageSize,
                SortBy = sortBy,
                TagFilter = tagFilter
            };

            VideoPageViewModel pageViewModel = await mediaHttpClient.GetVideoPage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);
            
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Video(int id, string tagFilter = "", int sortBy = 1)
        {
            Video video = await mediaHttpClient.GetVideo(id, Constants.DefaultTimezone);
            if (video == null)
            {
                return RedirectToAction("Index");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoItemViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            VideoViewModel videoViewModel = await mediaHttpClient.GetVideoViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone, tagFilter);
            
            model.SetPropertiesFromVideoViewModel(videoViewModel);
            
            if (model.CommentsCount > 0)
            {
                foreach (Comment cmnt in model.CommentsList)
                {
                    UserInfo commentAuthor = await userInfosHttpClient.GetUserInfoByUserId(cmnt.Author);
                    string commentAuthorProfilePicture = commentAuthor?.ProfilePicture ?? "";
                    commentAuthorProfilePicture = imageStore.UriFor(commentAuthorProfilePicture, "profiles");
                    cmnt.AuthorImage = commentAuthorProfilePicture;
                    cmnt.DisplayName = commentAuthor.FullName();
                }
            }
            if (model.IsCurrentUserProgenyAdmin)
            {
                model.ProgenyLocations = new List<Location>();
                model.ProgenyLocations = await locationsHttpClient.GetProgenyLocations(model.CurrentProgenyId, model.CurrentAccessLevel);
                model.LocationsList = new List<SelectListItem>();
                if (model.ProgenyLocations.Any())
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

        [AllowAnonymous]
        public IActionResult Youtube(string link)
        {
            return PartialView("Youtube", link);
        }

        public async Task<IActionResult> AddVideo()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            UploadVideoViewModel model = new(baseModel);

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
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
                model.Video.Owners = model.CurrentUser.UserEmail;
                model.Video.Author = model.CurrentUser.UserId;
                model.Video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

                model.ProgenyLocations = new List<Location>();
                model.ProgenyLocations = await locationsHttpClient.GetProgenyLocations(model.CurrentProgenyId, model.CurrentAccessLevel);
                model.LocationsList = new List<SelectListItem>();
                if (model.ProgenyLocations.Any())
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVideo(UploadVideoViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
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

            _ = await mediaHttpClient.AddVideo(videoToAdd);
            
            model.SetAccessLevelList();

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(VideoItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
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

            Video videoToUpdate = await mediaHttpClient.GetVideo(model.Video.VideoId, model.CurrentUser.Timezone);
            videoToUpdate.CopyPropertiesForUpdate(model.Video, true);
            
            if (model.Video.VideoTime != null)
            {
                videoToUpdate.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.Video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            
            _ = await mediaHttpClient.UpdateVideo(videoToUpdate);
            
            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.Video.VideoId, childId = model.Video.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            Video video = await mediaHttpClient.GetVideo(videoId, "");
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoItemViewModel model = new(baseModel);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.Video.ProgenyId);
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
                _ = await mediaHttpClient.DeleteVideo(model.Video.VideoId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            
            Comment comment = model.CreateComment((int)KinaUnaTypes.TimeLineType.Video);
            
            bool commentAdded = await mediaHttpClient.AddVideoComment(comment);

            if (commentAdded)
            {
                if (model.CurrentProgeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Videos/Video/" + model.ItemId + "?childId=" + model.CurrentProgenyId;
                    List<string> emails = model.CurrentProgeny.Admins.Split(",").ToList();
                    foreach (string toMail in emails)
                    {
                        await emailSender.SendEmailAsync(toMail, "New Comment on " + model.CurrentProgeny.NickName + "'s Video",
                           "A comment was added to " + model.CurrentProgeny.NickName + "'s video by " + comment.DisplayName + ":<br/><br/>" + comment.CommentText + "<br/><br/>Video Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                    
                }
            }

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.ItemId, childId = model.CurrentProgenyId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoComment(int commentId, int videoId, int progenyId)
        {
            await mediaHttpClient.DeleteVideoComment(commentId);

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = videoId, childId = progenyId });
        }
    }
}