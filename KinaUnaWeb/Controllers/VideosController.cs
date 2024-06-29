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
using KinaUnaWeb.Models.TypeScriptModels.Videos;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;
using KinaUnaWeb.Models.TypeScriptModels.Pictures;

namespace KinaUnaWeb.Controllers
{
    public class VideosController(
        IMediaHttpClient mediaHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IEmailSender emailSender,
        IViewModelSetupService viewModelSetupService,
        IConfiguration configuration)
        : Controller
    {
        private readonly string _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 16, int childId = 0, int sortBy = 2, string tagFilter = "", int year = 0, int month = 0, int day = 0)
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
                TagFilter = tagFilter,
                Year = year,
                Month = month,
                Day = day,
                VideosPageParameters = new VideosPageParameters
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

            VideoPageViewModel pageViewModel = await mediaHttpClient.GetVideoPage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);
            
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Video(int id, string tagFilter = "", int sortBy = 1, bool partialView = false)
        {
            Video video = await mediaHttpClient.GetVideo(id, Constants.DefaultTimezone);
            if (video == null)
            {
                return RedirectToAction("Index");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoItemViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey,
                PartialView = partialView
            };

            VideoViewModel videoViewModel = await mediaHttpClient.GetVideoViewModel(id, model.CurrentAccessLevel, sortBy, model.CurrentUser.Timezone, tagFilter);
            
            model.SetPropertiesFromVideoViewModel(videoViewModel);
            
            if (model.CommentsCount > 0)
            {
                foreach (Comment comment in model.CommentsList)
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
                model.ProgenyLocations = [];
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
                return PartialView("_VideoDetailsPartial", model);
            }

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

                model.ProgenyLocations = [];
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

            if (model.PartialView)
            {
                return Json(model);
            }

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

            if (!commentAdded) return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.ItemId, childId = model.CurrentProgenyId });

            if (model.CurrentProgeny == null) return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.ItemId, childId = model.CurrentProgenyId });

            string imgLink = Constants.WebAppUrl + "/Videos/Video/" + model.ItemId + "?childId=" + model.CurrentProgenyId;
            List<string> emails = [.. model.CurrentProgeny.Admins.Split(",")];
            foreach (string toMail in emails)
            {
                await emailSender.SendEmailAsync(toMail, "New Comment on " + model.CurrentProgeny.NickName + "'s Video",
                    "A comment was added to " + model.CurrentProgeny.NickName + "'s video by " + comment.DisplayName + ":<br/><br/>" + comment.CommentText + "<br/><br/>Video Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
            }

            if (model.PartialView)
            {
                return Json(model);
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GetVideoList([FromBody] VideosPageParameters parameters)
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

            VideosList videosList = new()
            {
                VideoItems = await mediaHttpClient.GetProgenyVideoList(baseModel.CurrentProgenyId, baseModel.CurrentAccessLevel)
            };

            videosList.VideoItems = videosList.VideoItems.OrderBy(p => p.VideoTime).ToList();

            if (!string.IsNullOrEmpty(parameters.TagFilter))
            {
                videosList.VideoItems = [.. videosList.VideoItems.Where(p => p.Tags != null && p.Tags.Contains(parameters.TagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }

            int videoCounter = 1;


            List<string> tagsList = [];
            foreach (Video video in videosList.VideoItems)
            {
                video.VideoNumber = videoCounter;

                videoCounter++;
                if (string.IsNullOrEmpty(video.Tags)) continue;

                List<string> videosPageViewModelTagsList = [.. video.Tags.Split(',')];
                foreach (string tagString in videosPageViewModelTagsList)
                {
                    string trimmedTag = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                    if (!string.IsNullOrEmpty(trimmedTag) && !tagsList.Contains(trimmedTag))
                    {
                        tagsList.Add(trimmedTag);
                    }
                }
            }
            videosList.TagsList = tagsList;

            DateTime currentDateTime = DateTime.UtcNow;
            DateTime firstItemTime = videosList.VideoItems.Where(p => p.VideoTime.HasValue).Min(t => t.VideoTime) ?? currentDateTime;

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

                videosList.VideoItems = videosList.VideoItems.Where(t => t.VideoTime <= startDate).OrderByDescending(p => p.VideoTime).ToList();
            }
            else
            {
                startDate = new DateTime(parameters.Year, parameters.Month, parameters.Day, 0, 0, 0);
                startDate = TimeZoneInfo.ConvertTimeToUtc(startDate, TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
                videosList.VideoItems = videosList.VideoItems.Where(t => t.VideoTime >= startDate).OrderBy(p => p.VideoTime).ToList();
            }

            firstItemTime = TimeZoneInfo.ConvertTimeFromUtc(firstItemTime, TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
            videosList.FirstItemYear = firstItemTime.Year;

            int skip = (parameters.CurrentPageNumber - 1) * parameters.ItemsPerPage;
            videosList.AllItemsCount = videosList.VideoItems.Count;
            videosList.RemainingItemsCount = videosList.VideoItems.Count - skip - parameters.ItemsPerPage;
            videosList.VideoItems = videosList.VideoItems.Skip(skip).Take(parameters.ItemsPerPage).ToList();
            videosList.TotalPages = (int)Math.Ceiling((double)videosList.AllItemsCount / parameters.ItemsPerPage);
            videosList.CurrentPageNumber = parameters.CurrentPageNumber;

            foreach (Video vid in videosList.VideoItems)
            {
                if (vid.VideoTime.HasValue)
                {
                    vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(baseModel.CurrentUser.Timezone));
                }
            }
            return Json(videosList);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> GetVideoElement([FromBody] VideoViewModel videoViewModel)
        {
            Video video = await mediaHttpClient.GetVideo(videoViewModel.VideoId, Constants.DefaultTimezone);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), video.ProgenyId);
            VideoViewModel videoViewModelData = await mediaHttpClient.GetVideoElement(videoViewModel.VideoId);
            VideoItemViewModel model = new(baseModel);

            model.SetPropertiesFromVideoViewModel(videoViewModelData);
            model.VideoNumber = videoViewModel.VideoNumber;
            model.Video.VideoNumber = videoViewModel.VideoNumber;
            return PartialView("_GetVideoElementPartial", model);
        }
    }
}