using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWebBlazor.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KinaUnaWebBlazor.Pages
{
    public class SetLanguageModel(IWebHostEnvironment env, ILanguagesHttpClient languagesHttpClient) : PageModel
    {
        public async Task<IActionResult> OnGet(int languageId, string returnUrl)
        {
            if (languageId > 0)
            {
                KinaUnaLanguage? language = await languagesHttpClient.GetLanguage(languageId);

                string cultureString = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language.CodeToLongFormat()));
                if (env.IsDevelopment())
                {
                    Response.Cookies.Append(
                        Constants.LanguageCookieName,
                        cultureString,
                        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Domain = "", IsEssential = true }
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

            Response.SetLanguageCookie(languageId.ToString());

            return Redirect(returnUrl);
        }
    }
}
