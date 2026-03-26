using KinaUna.OpenIddict.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;

namespace KinaUna.OpenIddict.Controllers
{
    public class HomeController(IWebHostEnvironment env) : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
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
            CookieOptions cookieOptions = new() { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "." + Constants.AppRootDomain };
            if (env.IsDevelopment())
            {
                cookieOptions.Expires = DateTimeOffset.UtcNow.AddYears(1);
            }
            else if (env.IsStaging())
            {
                cookieOptions.Expires = DateTimeOffset.UtcNow.AddYears(1);
                cookieOptions.Domain = ".azurewebsites.net";
            }
            Response.Cookies.Append(
                Constants.LanguageCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                cookieOptions
            );
            
            // Ensure the returnUrl is local to prevent open redirect vulnerabilities
            if (!IsReturnUrlSafe(returnUrl))
            {
                returnUrl = Url.Action("Index", "Home") ?? "/";
            }

            return Redirect(returnUrl);
        }



        [AllowAnonymous]
        [HttpGet]
        public IActionResult SetLanguageId(string languageId, string returnUrl)
        {
            Response.SetLanguageCookie(languageId);
            // Ensure the returnUrl is local to prevent open redirect vulnerabilities
            if (!IsReturnUrlSafe(returnUrl))
            {
                returnUrl = Url.Action("Index", "Home") ?? "/";
            }

            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? errorMessage)
        {
            return View(new ErrorViewModel { ErrorMessage = errorMessage, RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Validates that a return URL is safe (local to the application).
        /// Prevents open redirect attacks via crafted ReturnUrl values.
        /// </summary>
        private bool IsReturnUrlSafe(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return false; // No return URL provided, default to home page
            }

            // Url.IsLocalUrl rejects absolute URIs and protocol-relative URLs like "//evil.com"
            return Url.IsLocalUrl(returnUrl);
        }
    }
}
