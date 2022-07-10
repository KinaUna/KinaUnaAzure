using KinaUnaWeb.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private int _progId = Constants.DefaultChildId;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IWebHostEnvironment _env;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public HomeController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, IWebHostEnvironment env, IUserInfosHttpClient userInfosHttpClient, ICalendarsHttpClient calendarsHttpClient,
            IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _env = env;
            _userInfosHttpClient = userInfosHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 0)
        {
            int childId = id;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (childId == 0 && userinfo.ViewChild > 0)
                {
                    _progId = userinfo.ViewChild;
                }
            }
            else
            {
                _progId = Constants.DefaultChildId;
            }
            string userTimeZone = userinfo.Timezone;
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = Constants.DefaultTimezone;
            }
            

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            if (progeny.Name == "401")
            {
                var returnUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return RedirectToAction("CheckOut", "Account", new{returnUrl});
            }
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(_progId);

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
                userAccessLevel = (int)AccessLevel.Private;
            }

            if (progeny.BirthDay.HasValue)
            {
                progeny.BirthDay = DateTime.SpecifyKind(progeny.BirthDay.Value, DateTimeKind.Unspecified);
            }
            
            HomeFeedViewModel feedModel = new HomeFeedViewModel();
            feedModel.UserAccessLevel = (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    feedModel.UserAccessLevel = userAccess.AccessLevel;
                }
                else
                {
                    ViewBag.OriginalProgeny = progeny;
                    progeny = await _progenyHttpClient.GetProgeny(Constants.DefaultChildId);
                }
            }
            if (progeny.IsInAdminList(userEmail))
            {
                feedModel.UserAccessLevel = (int)AccessLevel.Private;
            }
            
            BirthTime progBirthTime;
            if (!string.IsNullOrEmpty(progeny.NickName) && progeny.BirthDay.HasValue && feedModel.UserAccessLevel < (int)AccessLevel.Public)
            {
                progBirthTime = new BirthTime(progeny.BirthDay.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }
            else
            {
                progBirthTime = new BirthTime(new DateTime(2018, 02, 18, 18, 02, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }

            feedModel.CurrentTime = progBirthTime.CurrentTime;
            feedModel.Years = progBirthTime.CalcYears();
            feedModel.Months = progBirthTime.CalcMonths();
            feedModel.Weeks = progBirthTime.CalcWeeks();
            feedModel.Days = progBirthTime.CalcDays();
            feedModel.Hours = progBirthTime.CalcHours();
            feedModel.Minutes = progBirthTime.CalcMinutes();
            feedModel.NextBirthday = progBirthTime.CalcNextBirthday();
            feedModel.MinutesMileStone = progBirthTime.CalcMileStoneMinutes();
            feedModel.HoursMileStone = progBirthTime.CalcMileStoneHours();
            feedModel.DaysMileStone = progBirthTime.CalcMileStoneDays();
            feedModel.WeeksMileStone = progBirthTime.CalcMileStoneWeeks();

            
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = (int)AccessLevel.Public;
            tempPicture.PictureLink600 = $"https://{Request.Host}{Request.PathBase}" + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            Picture displayPicture = tempPicture;

            if (feedModel.UserAccessLevel < (int)AccessLevel.Public)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(progeny.Id, feedModel.UserAccessLevel, userTimeZone);
            }
            PictureTime picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), new DateTime(2018, 02, 18, 20, 18, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            if (feedModel.UserAccessLevel == (int)AccessLevel.Public || displayPicture == null)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(Constants.DefaultChildId, feedModel.UserAccessLevel, userTimeZone);
                if(displayPicture == null)
                {
                    displayPicture = tempPicture;
                }
                if (!displayPicture.PictureLink600.StartsWith("https://"))
                {
                    displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);
                }

                feedModel.ImageLink600 = displayPicture.PictureLink600;
                feedModel.ImageId = displayPicture.PictureId;
                picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), displayPicture.PictureTime, TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                feedModel.Tags = displayPicture.Tags;
                feedModel.Location = displayPicture.Location;
                feedModel.PicTimeValid = false;
            }
            else
            {
                if (!displayPicture.PictureLink600.StartsWith("https://"))
                {
                    displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);
                }

                feedModel.ImageLink600 = displayPicture.PictureLink600;
                feedModel.ImageId = displayPicture.PictureId;
                if (displayPicture.PictureTime != null && progeny.BirthDay.HasValue)
                {
                    picTime = new PictureTime(progeny.BirthDay.Value,
                        displayPicture.PictureTime,
                        TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                    feedModel.PicTimeValid = true;
                }

                feedModel.Tags = displayPicture.Tags;
                feedModel.Location = displayPicture.Location;
            }
            feedModel.PicTime = picTime.PictureDateTime;
            feedModel.PicYears = picTime.CalcYears();
            feedModel.PicMonths = picTime.CalcMonths();
            feedModel.PicWeeks = picTime.CalcWeeks();
            feedModel.PicDays = picTime.CalcDays();
            feedModel.PicHours = picTime.CalcHours();
            feedModel.PicMinutes = picTime.CalcMinutes();
            feedModel.Progeny = progeny;
            feedModel.EventsList = new List<CalendarItem>();
            feedModel.EventsList = await _calendarsHttpClient.GetUpcomingEvents(_progId, userAccessLevel);
            
            foreach (CalendarItem eventItem in feedModel.EventsList)
            {
                if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
                {
                    eventItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    eventItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
            }

            feedModel.LatestPosts = new TimeLineViewModel();
            feedModel.LatestPosts.TimeLineItems = new List<TimeLineItem>();
            feedModel.LatestPosts.TimeLineItems = await _progenyHttpClient.GetProgenyLatestPosts(_progId, userAccessLevel);
            if (feedModel.LatestPosts.TimeLineItems.Any())
            {
                feedModel.LatestPosts.TimeLineItems = feedModel.LatestPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).Take(5).ToList();
            }

            feedModel.YearAgoPosts = new TimeLineViewModel();
            feedModel.YearAgoPosts.TimeLineItems = new List<TimeLineItem>();
            feedModel.YearAgoPosts.TimeLineItems = await _progenyHttpClient.GetProgenyYearAgo(_progId, userAccessLevel);
            if (feedModel.YearAgoPosts.TimeLineItems.Any())
            {
                feedModel.YearAgoPosts.TimeLineItems = feedModel.YearAgoPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).ToList();
            }

            return View(feedModel);
        }

        [Authorize]
        public IActionResult RequestAccess(int childId)
        {
            // ToDo: Implement access requests
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> About(int languageId = 0)
        {
            AboutViewModel model = new AboutViewModel();
            if (languageId == 0)
            {
                model.LanguageId = Request.GetLanguageIdFromCookie();
            }
            else
            {
                model.LanguageId = languageId;
                Response.SetLanguageCookie(languageId.ToString());
            }
            model.CurrentUser = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            
            return View(model);
        }
        

        [AllowAnonymous]
        public async Task<IActionResult> Privacy(int languageId = 0)
        {
            AboutViewModel model = new AboutViewModel();
            if (languageId == 0)
            {
                model.LanguageId = Request.GetLanguageIdFromCookie();
            }
            else
            {
                model.LanguageId = languageId;
                Response.SetLanguageCookie(languageId.ToString());
            }

            model.CurrentUser = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Terms(int languageId = 0)
        {

            AboutViewModel model = new AboutViewModel();
            if (languageId == 0)
            {
                model.LanguageId = Request.GetLanguageIdFromCookie();
            }
            else
            {
                model.LanguageId = languageId;
                Response.SetLanguageCookie(languageId.ToString());
            }

            model.CurrentUser = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());

            return View(model);
        }
        
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetViewChild(int childId, string userId, string userEmail, string returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
                userinfo.ViewChild = childId;
                await _userInfosHttpClient.SetViewChild(userId, userinfo);
            }

            // return Redirect(returnUrl);
            return RedirectToAction("Index", new{ childId });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (_env.IsDevelopment())
            {
                Response.Cookies.Append(
                    Constants.LanguageCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }
            else
            {
                Response.Cookies.Append(
                    Constants.LanguageCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "." + Constants.AppRootDomain }
                );
            }
            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult SetLanguageId(string languageId, string returnUrl)
        {
            Response.SetLanguageCookie(languageId);
            return Redirect(returnUrl);
        }


        [AllowAnonymous]
        public IActionResult Start()
        {
            return Redirect(Constants.WebAppUrl);
        }
        
    }
}
