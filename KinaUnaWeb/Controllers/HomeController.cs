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
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class HomeController(
        IMediaHttpClient mediaHttpClient,
        IWebHostEnvironment env,
        IUserInfosHttpClient userInfosHttpClient,
        ILanguagesHttpClient languagesHttpClient,
        IViewModelSetupService viewModelSetupService,
        IDistributedCache cache)
        : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            HomeFeedViewModel model = new(baseModel);
            
            if (model.CurrentProgeny.Name == "401")
            {
                string returnUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                return RedirectToAction("CheckOut", "Account", new{returnUrl});
            }

            model.SetBirthTimeData();
            
            if (model.CurrentAccessLevel < (int)AccessLevel.Public)
            {
                model.DisplayPicture = await mediaHttpClient.GetRandomPicture(model.CurrentProgeny.Id, model.CurrentAccessLevel, model.CurrentUser.Timezone);
            }

            if (model.CurrentAccessLevel == (int)AccessLevel.Public || model.DisplayPicture == null)
            {
                model.DisplayPicture = await mediaHttpClient.GetRandomPicture(Constants.DefaultChildId, model.CurrentAccessLevel, model.CurrentUser.Timezone) ?? model.CreateTempPicture($"https://{Request.Host}{Request.PathBase}");

                model.PicTimeValid = false;
            }
            else
            {
                if (model.DisplayPicture.PictureTime != null && model.CurrentProgeny.BirthDay.HasValue)
                {
                    model.PicTimeValid = true;
                }

            }

            model.DisplayPicture.PictureLink600 = model.DisplayPicture.GetPictureUrl(600);
            model.SetDisplayPictureData();
            
            model.SetPictureTimeData();
            
            return View(model);
        }

        [Authorize]
        public IActionResult RequestAccess()
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
            model.CurrentUser = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            
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

            model.CurrentUser = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());

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

            model.CurrentUser = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());

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
        public async Task<IActionResult> SetViewChild(int childId, int languageId = 1)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Index", new { childId });

            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            userinfo.ViewChild = childId;
            await userInfosHttpClient.SetViewChild(User.GetUserId(), userinfo);

            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userinfo.UserEmail.ToUpper() + "_progeny_" + 0);

            // return Redirect(returnUrl);
            return RedirectToAction("Index", new{ childId });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (env.IsDevelopment())
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
                KinaUnaLanguage language = await languagesHttpClient.GetLanguage(languageIdAsInt);
                
                string cultureString = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language.CodeToLongFormat()));
                if (env.IsDevelopment())
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
