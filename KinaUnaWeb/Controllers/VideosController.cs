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
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public VideosController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 8, int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            _progId = childId;
            if (id < 1)
            {
                id = 1;
            }
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? Constants.DefaultTimezone;
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = Constants.DefaultTimezone;
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            else
            {
                _progId = childId;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }


            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }


            VideoPageViewModel model = await _mediaHttpClient.GetVideoPage(pageSize, id, progeny.Id, userAccessLevel, sortBy, tagFilter, userTimeZone);
            model.Progeny = progeny;
            model.IsAdmin = _userIsProgenyAdmin;
            model.SortBy = sortBy;
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Video(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? Constants.DefaultTimezone;
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = Constants.DefaultTimezone;
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(id, userAccessLevel, sortBy, userinfo.Timezone);
            
            VideoViewModel model = new VideoViewModel();
            model.VideoId = video.VideoId;
            model.VideoType = video.VideoType;
            model.VideoTime = video.VideoTime;
            model.ProgenyId = video.ProgenyId;
            model.Progeny = progeny;
            model.Owners = video.Owners;
            model.VideoLink = video.VideoLink;
            model.ThumbLink = video.ThumbLink;
            model.AccessLevel = video.AccessLevel;
            model.Author = video.Author;
            model.AccessLevelListEn[video.AccessLevel].Selected = true;
            model.AccessLevelListDa[video.AccessLevel].Selected = true;
            model.AccessLevelListDe[video.AccessLevel].Selected = true;
            model.CommentThreadNumber = video.CommentThreadNumber;
            model.Tags = video.Tags;
            model.TagsList = video.TagsList;
            model.Location = video.Location;
            model.Latitude = video.Latitude;
            model.Longtitude = video.Longtitude;
            model.Altitude = video.Altitude;
            model.VideoNumber = video.VideoNumber;
            model.VideoCount = video.VideoCount;
            model.PrevVideo = video.PrevVideo;
            model.NextVideo = video.NextVideo;
            model.CommentsList = video.CommentsList;
            model.CommentsCount = video.CommentsList?.Count ?? 0;
            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            model.UserId = HttpContext.User.FindFirst("sub")?.Value ?? _defaultUser;
            model.IsAdmin = _userIsProgenyAdmin;
            if (video.Duration != null)
            {
                model.DurationHours = video.Duration.Value.Hours.ToString();
                model.DurationMinutes = video.Duration.Value.Minutes.ToString();
                model.DurationSeconds = video.Duration.Value.Seconds.ToString();
            }
            if (model.VideoTime != null && progeny.BirthDay.HasValue)
            {
                PictureTime picTime = new PictureTime(progeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)),
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                model.VidTimeValid = true;
                model.VidTime = model.VideoTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                model.VidYears = picTime.CalcYears();
                model.VidMonths = picTime.CalcMonths();
                model.VidWeeks = picTime.CalcWeeks();
                model.VidDays = picTime.CalcDays();
                model.VidHours = picTime.CalcHours();
                model.VidMinutes = picTime.CalcMinutes();
            }
            else
            {
                model.VidTimeValid = false;
                model.VidTime = "";
            }
            if (model.CommentsCount > 0)
            {
                foreach (Comment cmnt in model.CommentsList)
                {
                    UserInfo cmntAuthor = await _progenyHttpClient.GetUserInfoByUserId(cmnt.Author);
                    string authorImg = cmntAuthor?.ProfilePicture ?? "";
                    string authorName = "";
                    if (!String.IsNullOrEmpty(authorImg))
                    {
                        if (!authorImg.ToLower().StartsWith("http"))
                        {
                            authorImg = _imageStore.UriFor(authorImg, "profiles");
                        }
                    }
                    else
                    {
                        authorImg = "/photodb/profile.jpg";
                    }
                    cmnt.AuthorImage = authorImg;

                    if (!String.IsNullOrEmpty(cmntAuthor.FirstName))
                    {
                        authorName = cmntAuthor.FirstName;
                    }
                    if (!String.IsNullOrEmpty(cmntAuthor.MiddleName))
                    {
                        authorName = authorName + " " + cmntAuthor.MiddleName;
                    }
                    if (!String.IsNullOrEmpty(cmntAuthor.LastName))
                    {
                        authorName = authorName + " " + cmntAuthor.LastName;
                    }

                    authorName = authorName.Trim();
                    if (String.IsNullOrEmpty(authorName))
                    {
                        authorName = cmntAuthor.UserName;
                        if (String.IsNullOrEmpty(authorName))
                        {
                            authorName = cmnt.DisplayName;
                        }
                    }

                    cmnt.DisplayName = authorName;
                }
            }
            if (model.IsAdmin)
            {
                model.ProgenyLocations = new List<Location>();
                model.ProgenyLocations = await _progenyHttpClient.GetProgenyLocations(model.ProgenyId, userAccessLevel);
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

            return View(model);
        }
    }
}