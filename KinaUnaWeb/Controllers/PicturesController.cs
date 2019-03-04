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

namespace KinaUnaWeb.Controllers
{
    public class PicturesController : Controller
    {
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public PicturesController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore)
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

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }


            PicturePageViewModel model = await _mediaHttpClient.GetPicturePage(pageSize, id, progeny.Id, userAccessLevel, sortBy, tagFilter, userTimeZone);
            model.Progeny = progeny;
            model.IsAdmin = _userIsProgenyAdmin;
            model.SortBy = sortBy;
            model.PageSize = pageSize;
            foreach (Picture pic in model.PicturesList)
            {
                if (!pic.PictureLink600.StartsWith("https://"))
                {
                    pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Picture(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
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

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(id, userAccessLevel, sortBy, userTimeZone);
            if (!picture.PictureLink.StartsWith("https://"))
            {
                picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
            }
            
            PictureViewModel model = new PictureViewModel();
            model.PictureId = picture.PictureId;
            model.PictureTime = picture.PictureTime;
            model.ProgenyId = picture.ProgenyId;
            model.Progeny = progeny;
            model.Owners = picture.Owners;
            model.PictureLink = picture.PictureLink;
            model.AccessLevel = picture.AccessLevel;
            model.Author = picture.Author;
            model.AccessLevelListEn[picture.AccessLevel].Selected = true;
            model.AccessLevelListDa[picture.AccessLevel].Selected = true;
            model.AccessLevelListDe[picture.AccessLevel].Selected = true;
            model.CommentThreadNumber = picture.CommentThreadNumber;
            model.Tags = picture.Tags;
            model.TagsList = picture.TagsList;
            model.Location = picture.Location;
            model.Latitude = picture.Latitude;
            model.Longtitude = picture.Longtitude;
            model.Altitude = picture.Altitude;
            model.PictureNumber = picture.PictureNumber;
            model.PictureCount = picture.PictureCount;
            model.PrevPicture = picture.PrevPicture;
            model.NextPicture = picture.NextPicture;
            model.CommentsList = picture.CommentsList ?? new List<Comment>();
            model.CommentsCount = picture.CommentsList?.Count ?? 0;
            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            model.UserId = HttpContext.User.FindFirst("sub")?.Value ?? _defaultUser;
            model.IsAdmin = _userIsProgenyAdmin;
            if (model.PictureTime != null && progeny.BirthDay.HasValue)
            {
                PictureTime picTime = new PictureTime(progeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)),
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                model.PicTimeValid = true;
                model.PicTime = model.PictureTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace format string with global constant or user defined value
                model.PicYears = picTime.CalcYears();
                model.PicMonths = picTime.CalcMonths();
                model.PicWeeks = picTime.CalcWeeks();
                model.PicDays = picTime.CalcDays();
                model.PicHours = picTime.CalcHours();
                model.PicMinutes = picTime.CalcMinutes();
            }
            else
            {
                model.PicTimeValid = false;
                model.PicTime = "";
            }

            if (model.CommentsCount > 0)
            {
                foreach(Comment comment in model.CommentsList)
                {
                    UserInfo cmntAuthor = await _progenyHttpClient.GetUserInfoByUserId(comment.Author);
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
                    comment.AuthorImage = authorImg;

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
                            authorName = comment.DisplayName;
                        }
                    }

                    comment.DisplayName = authorName;
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