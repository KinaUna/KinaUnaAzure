using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Models.TypeScriptModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class HomeController(
        IMediaHttpClient mediaHttpClient,
        IWebHostEnvironment env,
        IUserInfosHttpClient userInfosHttpClient,
        IProgenyHttpClient progenyHttpClient,
        ILanguagesHttpClient languagesHttpClient,
        IViewModelSetupService viewModelSetupService,
        IDistributedCache cache)
        : Controller
    {
        /// <summary>
        /// The Home Index page.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to view data for.</param>
        /// <param name="familyId">The Id of the Family to view data for.</param>
        /// <returns>View with HomeFeedViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int familyId = 0)
        {

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId);
            HomeFeedViewModel model = new(baseModel);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ProgenyTrivia([FromBody] TimelineRequest request)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                request.ProgenyId = Constants.DefaultChildId;
            }
            else
            {
                if (request.ProgenyId == 0)
                {
                    // Select a random Progeny if none is selected.
                    if (request.Progenies.Count > 0)
                    {
                        Random rand = new();
                        request.ProgenyId = request.Progenies[rand.Next(request.Progenies.Count)];
                    }
                    else
                    {
                        request.ProgenyId = Constants.DefaultChildId;
                    }
                }
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), request.ProgenyId, 0, false);
            HomeFeedViewModel model = new(baseModel);
            model.ProgenyList = [];
            if (request.Progenies.Count > 0)
            {
                foreach (int requestProgenyId in request.Progenies)
                {
                    Progeny progeny = await progenyHttpClient.GetProgeny(requestProgenyId);
                    if (progeny != null && progeny.Id > 0)
                    {
                        model.TriviaProgenies.Add(progeny);
                    }
                }
            }
            model.SetBirthTimeData();

            model.DisplayPicture = await mediaHttpClient.GetRandomPicture(model.CurrentProgeny.Id, model.CurrentUser.Timezone);

            if (model.DisplayPicture == null)
            {
                model.DisplayPicture = await mediaHttpClient.GetRandomPicture(Constants.DefaultChildId, model.CurrentUser.Timezone) ?? model.CreateTempPicture($"https://{Request.Host}{Request.PathBase}");

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

            return PartialView("_ProgenyTriviaPartial", model);
        }

        /// <summary>
        /// Not implemented. This action should handle access requests.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public IActionResult RequestAccess()
        {
            // ToDo: Implement access requests
            return View();
        }

        /// <summary>
        /// The About page.
        /// </summary>
        /// <param name="languageId">The Id of the language to show the about page in.</param>
        /// <returns>View with AboutViewModel.</returns>
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
        
        /// <summary>
        /// Privacy page.
        /// </summary>
        /// <param name="languageId">The Id of the language to show the privacy page in.</param>
        /// <returns>View with AboutViewModel.</returns>
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

        /// <summary>
        /// Terms and conditions page.
        /// </summary>
        /// <param name="languageId">The Id of the language to show the terms and conditions page in.</param>
        /// <returns>View with AboutViewModel.</returns>
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

        /// <summary>
        /// Error page.
        /// </summary>
        /// <returns>View with ErrorViewModel.</returns>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// HttpPost action to set the current Progeny as default to view.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to set as default.</param>
        /// <param name="languageId">The current language's Id.</param>
        /// <returns>Redirects to Home page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetViewChild(int childId, int languageId = 1)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Index", new { childId });

            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            userinfo.ViewChild = childId;
            await userInfosHttpClient.UpdateUserInfo(userinfo);

            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userinfo.UserEmail.ToUpper() + "_progeny_" + 0);

            return RedirectToAction("Index", new{ childId });
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultProgeny([FromBody] SetProgenyRequest request)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return BadRequest();

            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(User.GetEmail());
            userinfo.ViewChild = request.ProgenyId;
            await userInfosHttpClient.UpdateUserInfo(userinfo);

            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + request.LanguageId + "_user_" + userinfo.UserEmail.ToUpper() + "_progeny_" + 0);

            return Ok();
        }
        /// <summary>
        /// HttpPost action to set the current language.
        /// </summary>
        /// <param name="culture">The culture code of the language, i.e. en-US, de-DE, da-DK.</param>
        /// <param name="returnUrl">The page to return to.</param>
        /// <returns>Redirects to returnUrl.</returns>
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
                if (env.IsStaging())
                {
                    Response.Cookies.Append(
                        Constants.LanguageCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = ".azurewebsites.net" }
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
                
            }
            return Redirect(returnUrl);
        }

        /// <summary>
        /// HttpGet action to set the current language.
        /// </summary>
        /// <param name="languageId">The Id of the language to set.</param>
        /// <param name="returnUrl">The page to return to.</param>
        /// <returns>Redirects to returnUrl.</returns>
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
                    if (env.IsStaging())
                    {
                        Response.Cookies.Append(
                            Constants.LanguageCookieName,
                            cultureString,
                            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = ".azurewebsite.net", IsEssential = true }
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
            }
            
            Response.SetLanguageCookie(languageId);
            return Redirect(returnUrl);
        }
        
    }
}
