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

namespace KinaUnaWeb.Controllers
{
    public class VideosController(
        IMediaHttpClient mediaHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IUserAccessHttpClient userAccessHttpClient,
        IEmailSender emailSender,
        IViewModelSetupService viewModelSetupService,
        IConfiguration configuration)
        : Controller
    {
        private readonly string _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");

        /// <summary>
        /// Video gallery page.
        /// </summary>
        /// <param name="id">The current page number.</param>
        /// <param name="pageSize">Number of videos per page.</param>
        /// <param name="childId">The Id of the Progeny to show videos for.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first. 1 >= newest first.</param>
        /// <param name="tagFilter">Filter by Tag content. If empty string include all videos.</param>
        /// <param name="year">Start year.</param>
        /// <param name="month">Start month.</param>
        /// <param name="day">Start day.</param>
        /// <param name="videoId">The Id of the video to show in a popup/modal, If 0, no popup is shown.</param>
        /// <returns>View with VideoListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 16, int childId = 0, int sortBy = 2, string tagFilter = "", int year = 0, int month = 0, int day = 0, int videoId = 0)
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
                },
                VideoId = videoId
            };

            VideoPageViewModel pageViewModel = await mediaHttpClient.GetVideoPage(pageSize, id, model.CurrentProgenyId, model.CurrentAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.SetPropertiesFromPageViewModel(pageViewModel);
            
            
            return View(model);
        }

        /// <summary>
        /// Video details page or PartialView.
        /// </summary>
        /// <param name="id">The VideoId of the Video to show.</param>
        /// <param name="tagFilter">The active tag filter.</param>
        /// <param name="sortBy">The active sort order.</param>
        /// <param name="partialView">If true, return a PartialView for use in popups/modals.</param>
        /// <returns>View or PartialView with VideoItemViewModel.</returns>
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
            model.Video.Progeny.PictureLink = model.Video.Progeny.GetProfilePictureUrl();
            if (partialView)
            {
                return PartialView("_VideoDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page for showing a Youtube video.
        /// Used by mobile clients to embed in WebView.
        /// </summary>
        /// <param name="link">The Youtube video id of the video.</param>
        /// <returns>View</returns>
        [AllowAnonymous]
        public IActionResult Youtube(string link)
        {
            return PartialView("Youtube", link);
        }

        /// <summary>
        /// Page for adding a new video item.
        /// </summary>
        /// <returns>View with UploadVideoViewModel.</returns>
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

        /// <summary>
        /// HttpPost method for adding a new video item.
        /// </summary>
        /// <param name="model">UploadVideoViewModel with the properties for the Video to add.</param>
        /// <returns>View with UploadVideoViewModel.</returns>
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

        /// <summary>
        /// HttpPost method for updating a Video.
        /// </summary>
        /// <param name="model">VideoViewModel with the updated properties of the Video.</param>
        /// <returns>Redirects to the Videos/Video page of the item.</returns>
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

        /// <summary>
        /// Page for deleting a Video item.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video to delete.</param>
        /// <returns>View with VideoItemViewModel.</returns>
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

        /// <summary>
        /// HttpPost method for deleting a Video item.
        /// </summary>
        /// <param name="model">VideoItemViewModel with the properties of the Video to delete.</param>
        /// <returns>Redirects to Video/Index page.</returns>
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

        /// <summary>
        /// HttpPost method for adding a new comment to a Video.
        /// </summary>
        /// <param name="model">CommentViewModel with the properties of the Comment to add.</param>
        /// <returns>If model.PartialView is true, Json of the updated model, else redirects to the Videos/Video page for the Video item. </returns>
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

        /// <summary>
        /// HttpPost method for deleting a comment from a Video.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to delete.</param>
        /// <param name="videoId">The VideoId of the Video the Comment belongs to.</param>
        /// <param name="progenyId">The Id of the Progeny the Video belongs to.</param>
        /// <returns>Redirect to the Videos/Video page for the Video the Comment belongs to.</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoComment(int commentId, int videoId, int progenyId)
        {
            await mediaHttpClient.DeleteVideoComment(commentId);

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = videoId, childId = progenyId });
        }

        /// <summary>
        /// HttpPost method for getting a list of Video for a Progeny.
        /// For Ajax calls.
        /// </summary>
        /// <param name="parameters">VideoPageParameters object.</param>
        /// <returns>Json of VideosList object.</returns>
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

            // Todo: Refactor to process the videos list in the API.
            List<UserAccess> userAccessList = await userAccessHttpClient.GetUserAccessList(baseModel.CurrentUser.UserEmail);

            VideosList videosList = new();

            foreach (int progenyId in parameters.Progenies)
            {
                int accessLevel = userAccessList.SingleOrDefault(u => u.ProgenyId == progenyId)?.AccessLevel ?? 5;
                List<Video> progenyVideos = await mediaHttpClient.GetProgenyVideoList(baseModel.CurrentProgenyId, accessLevel);
                if (progenyVideos != null)
                {
                    videosList.VideoItems.AddRange(progenyVideos);
                }
            }
            
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

        /// <summary>
        /// HttpPost method for getting a Video element.
        /// For inserting HTML using Ajax calls.
        /// </summary>
        /// <param name="videoViewModel">VideoViewModel.</param>
        /// <returns>PartialView with VideoItemViewModel.</returns>
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