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
            CookieOptions cookieOptions = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "." + Constants.AppRootDomain };
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

            return Redirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult SetLanguageId(string languageId, string returnUrl)
        {
            Response.SetLanguageCookie(languageId);
            return Redirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? errorMessage)
        {
            return View(new ErrorViewModel { ErrorMessage = errorMessage, RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
