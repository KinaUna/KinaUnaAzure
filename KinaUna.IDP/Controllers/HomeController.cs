using IdentityServer4.Services;
using KinaUna.IDP.Models;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KinaUna.IDP.Controllers
{
    //[EnableCors("KinaUnaCors")]
    public class HomeController(IIdentityServerInteractionService interaction, IRedirectService redirectSvc, IWebHostEnvironment env) : Controller
    {
        public IActionResult Index(string returnUrl)
        {
            return View();
        }

        public IActionResult ReturnToOriginalApplication(string returnUrl)
        {
            if (returnUrl != null)
                return Redirect(redirectSvc.ExtractRedirectUriFromReturnUrl(returnUrl));
            else
                return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            ErrorViewModel vm = new ErrorViewModel();

            // retrieve error details from identityserver
            ErrorMessage message = await interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Terms()
        {
            return View();
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
        public IActionResult SetLanguageId(string languageId, string returnUrl)
        {
            Response.SetLanguageCookie(languageId);
            return Redirect(returnUrl);
        }
    }
}