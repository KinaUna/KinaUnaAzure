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
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IWebHostEnvironment _env;
        private readonly ILanguagesHttpClient _languagesHttpClient;
        public HomeController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, IWebHostEnvironment env, IUserInfosHttpClient userInfosHttpClient, ICalendarsHttpClient calendarsHttpClient,
            IUserAccessHttpClient userAccessHttpClient, ILanguagesHttpClient languagesHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _env = env;
            _userInfosHttpClient = userInfosHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _languagesHttpClient = languagesHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 0)
        {
            HomeFeedViewModel model = new HomeFeedViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);


            if (id == 0 && model.CurrentUser.ViewChild > 0)
            {
                id = model.CurrentUser.ViewChild;
            }

            if (id == 0)
            {
                id = Constants.DefaultChildId;
            }
            

            Progeny progeny = await _progenyHttpClient.GetProgeny(id);
            if (progeny.Name == "401")
            {
                string returnUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return RedirectToAction("CheckOut", "Account", new{returnUrl});
            }
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(id);

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
            
            
            model.UserAccessLevel = (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    model.UserAccessLevel = userAccess.AccessLevel;
                }
                else
                {
                    ViewBag.OriginalProgeny = progeny;
                    progeny = await _progenyHttpClient.GetProgeny(Constants.DefaultChildId);
                }
            }
            if (progeny.IsInAdminList(userEmail))
            {
                model.UserAccessLevel = (int)AccessLevel.Private;
            }
            
            BirthTime progBirthTime;
            if (!string.IsNullOrEmpty(progeny.NickName) && progeny.BirthDay.HasValue && model.UserAccessLevel < (int)AccessLevel.Public)
            {
                progBirthTime = new BirthTime(progeny.BirthDay.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }
            else
            {
                progBirthTime = new BirthTime(new DateTime(2018, 02, 18, 18, 02, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            }

            model.CurrentTime = progBirthTime.CurrentTime;
            model.Years = progBirthTime.CalcYears();
            model.Months = progBirthTime.CalcMonths();
            model.Weeks = progBirthTime.CalcWeeks();
            model.Days = progBirthTime.CalcDays();
            model.Hours = progBirthTime.CalcHours();
            model.Minutes = progBirthTime.CalcMinutes();
            model.NextBirthday = progBirthTime.CalcNextBirthday();
            model.MinutesMileStone = progBirthTime.CalcMileStoneMinutes();
            model.HoursMileStone = progBirthTime.CalcMileStoneHours();
            model.DaysMileStone = progBirthTime.CalcMileStoneDays();
            model.WeeksMileStone = progBirthTime.CalcMileStoneWeeks();

            
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = (int)AccessLevel.Public;
            tempPicture.PictureLink600 = $"https://{Request.Host}{Request.PathBase}" + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            Picture displayPicture = tempPicture;

            if (model.UserAccessLevel < (int)AccessLevel.Public)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(progeny.Id, model.UserAccessLevel, model.CurrentUser.Timezone);
            }
            PictureTime picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), new DateTime(2018, 02, 18, 20, 18, 00), TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
            if (model.UserAccessLevel == (int)AccessLevel.Public || displayPicture == null)
            {
                displayPicture = await _mediaHttpClient.GetRandomPicture(Constants.DefaultChildId, model.UserAccessLevel, model.CurrentUser.Timezone);
                if(displayPicture == null)
                {
                    displayPicture = tempPicture;
                }

                displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);

                model.ImageLink600 = displayPicture.PictureLink600;
                model.ImageId = displayPicture.PictureId;
                picTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00), displayPicture.PictureTime, TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                model.Tags = displayPicture.Tags;
                model.Location = displayPicture.Location;
                model.PicTimeValid = false;
            }
            else
            {
                displayPicture.PictureLink600 = _imageStore.UriFor(displayPicture.PictureLink600);

                model.ImageLink600 = displayPicture.PictureLink600;
                model.ImageId = displayPicture.PictureId;
                if (displayPicture.PictureTime != null && progeny.BirthDay.HasValue)
                {
                    picTime = new PictureTime(progeny.BirthDay.Value,
                        displayPicture.PictureTime,
                        TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                    model.PicTimeValid = true;
                }

                model.Tags = displayPicture.Tags;
                model.Location = displayPicture.Location;
            }
            model.PicTime = picTime.PictureDateTime;
            model.PicYears = picTime.CalcYears();
            model.PicMonths = picTime.CalcMonths();
            model.PicWeeks = picTime.CalcWeeks();
            model.PicDays = picTime.CalcDays();
            model.PicHours = picTime.CalcHours();
            model.PicMinutes = picTime.CalcMinutes();
            model.Progeny = progeny;
            model.EventsList = new List<CalendarItem>();
            model.EventsList = await _calendarsHttpClient.GetUpcomingEvents(id, userAccessLevel);
            
            foreach (CalendarItem eventItem in model.EventsList)
            {
                if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
                {
                    eventItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    eventItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
            }

            model.LatestPosts = new TimeLineViewModel();
            model.LatestPosts.LanguageId = model.LanguageId; 
            model.LatestPosts.TimeLineItems = new List<TimeLineItem>();
            model.LatestPosts.TimeLineItems = await _progenyHttpClient.GetProgenyLatestPosts(id, userAccessLevel);
            if (model.LatestPosts.TimeLineItems.Any())
            {
                model.LatestPosts.TimeLineItems = model.LatestPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).Take(5).ToList();
            }

            model.YearAgoPosts = new TimeLineViewModel();
            model.YearAgoPosts.LanguageId = model.LanguageId;
            model.YearAgoPosts.TimeLineItems = new List<TimeLineItem>();
            model.YearAgoPosts.TimeLineItems = await _progenyHttpClient.GetProgenyYearAgo(id, userAccessLevel);
            if (model.YearAgoPosts.TimeLineItems.Any())
            {
                model.YearAgoPosts.TimeLineItems = model.YearAgoPosts.TimeLineItems.OrderByDescending(t => t.ProgenyTime).ToList();
            }

            return View(model);
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
        public async Task<IActionResult> SetLanguageId(string languageId, string returnUrl)
        {
            bool languageIdParsed = int.TryParse(languageId, out int languageIdAsInt);
            if (languageIdParsed)
            {
                KinaUnaLanguage language = await _languagesHttpClient.GetLanguage(languageIdAsInt);
                
                string cultureString = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language.CodeToLongFormat()));
                if (_env.IsDevelopment())
                {
                    Response.Cookies.Append(
                    Constants.LanguageCookieName,
                        cultureString,
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "", IsEssential = true}
                    );
                }
                else
                {
                    Response.Cookies.Append(
                    Constants.LanguageCookieName,
                        cultureString,
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "." + Constants.AppRootDomain, IsEssential = true }
                    );
                }
            }
            
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
