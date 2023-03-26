using KinaUnaWeb.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
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
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IWebHostEnvironment _env;
        private readonly ILanguagesHttpClient _languagesHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;
        public HomeController(IMediaHttpClient mediaHttpClient, ImageStore imageStore, IWebHostEnvironment env, IUserInfosHttpClient userInfosHttpClient,
            ICalendarsHttpClient calendarsHttpClient, ILanguagesHttpClient languagesHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _env = env;
            _userInfosHttpClient = userInfosHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _languagesHttpClient = languagesHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            HomeFeedViewModel model = new(baseModel);
            
            if (model.CurrentProgeny.Name == "401")
            {
                string returnUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return RedirectToAction("CheckOut", "Account", new{returnUrl});
            }

            model.SetBirthTimeData();
            
            if (model.CurrentAccessLevel < (int)AccessLevel.Public)
            {
                model.DisplayPicture = await _mediaHttpClient.GetRandomPicture(model.CurrentProgeny.Id, model.CurrentAccessLevel, model.CurrentUser.Timezone);
            }

            if (model.CurrentAccessLevel == (int)AccessLevel.Public || model.DisplayPicture == null)
            {
                model.DisplayPicture = await _mediaHttpClient.GetRandomPicture(Constants.DefaultChildId, model.CurrentAccessLevel, model.CurrentUser.Timezone);
                if(model.DisplayPicture == null)
                {
                    model.DisplayPicture = model.CreateTempPicture($"https://{Request.Host}{Request.PathBase}");
                }
                
                model.PicTimeValid = false;
            }
            else
            {
                if (model.DisplayPicture.PictureTime != null && model.CurrentProgeny.BirthDay.HasValue)
                {
                    model.PicTimeValid = true;
                }

            }

            model.DisplayPicture.PictureLink600 = _imageStore.UriFor(model.DisplayPicture.PictureLink600);
            model.SetDisplayPictureData();
            
            model.SetPictureTimeData();
            
            model.EventsList = await _calendarsHttpClient.GetUpcomingEvents(model.CurrentProgenyId, model.CurrentAccessLevel, model.CurrentUser.Timezone);
            
            model.LatestPosts = await _viewModelSetupService.GetLatestPostTimeLineModel(model.CurrentProgenyId, model.CurrentAccessLevel, model.LanguageId);

            model.YearAgoPosts = await _viewModelSetupService.GetYearAgoPostsTimeLineModel(model.CurrentProgenyId, model.CurrentAccessLevel, model.LanguageId);

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
            AboutViewModel model = new();
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
            AboutViewModel model = new();
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

            AboutViewModel model = new();
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
        public async Task<IActionResult> SetViewChild(int childId, string returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(User.GetEmail());
                userinfo.ViewChild = childId;
                await _userInfosHttpClient.SetViewChild(User.GetUserId(), userinfo);
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
