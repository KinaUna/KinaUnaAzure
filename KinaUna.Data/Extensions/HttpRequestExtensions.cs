using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace KinaUna.Data.Extensions
{
    public static class HttpRequestExtensions
    {
        public static int GetLanguageIdFromCookie(this HttpRequest request)
        {
            int languageId = 0;
            if (request.Cookies.TryGetValue("languageId", out string languageIdText))
            {
                if (!int.TryParse(languageIdText, out languageId))
                {
                    languageId = request.GetLanguageIdFromBrowser();
                }
            }

            if (languageId == 0)
            {
                languageId = 1;
            }

            return languageId;
        }

        public static void SetLanguageCookie(this HttpResponse response, string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                languageId = "1";
            }
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddYears(1);
            option.Domain = ".kinauna.com";
            option.IsEssential = true;
            option.Secure = true;
            option.SameSite = SameSiteMode.Lax;
            #if DEBUG
            option.Domain = "";
            #endif
            response.Cookies.Append("languageId", languageId, option);  
        }

        private static int GetLanguageIdFromBrowser(this HttpRequest request)
        {
            int languageId = 1;

            try
            {
                string[] userLanguages = request.GetTypedHeaders().AcceptLanguage.OrderByDescending(x => x.Quality ?? 1).Select(x => x.Value.ToString()).ToArray();
                if (userLanguages.Any())
                {
                    string firstLang = userLanguages.FirstOrDefault();

                    if (firstLang != null && firstLang.StartsWith("de"))
                    {
                        languageId = 2;
                    }

                    if (firstLang != null && firstLang.StartsWith("da"))
                    {
                        languageId = 3;
                    }
                }
            }
            catch (NullReferenceException)
            {
                languageId = 1;
            }

            return languageId;
        }


        public static int GetGdprCookie(this HttpRequest request)
        {
            // 0 = not set, only allow essential cookies.
            // 1 = only essential cookies.
            // 2 = allow google maps.
            // 3 = allow  all

            if (request.Cookies.TryGetValue("cookieconsent", out string gdprText))
            {
                if (string.IsNullOrEmpty(gdprText))
                {
                    return 0;
                }

                if (gdprText.Contains("1"))
                {
                    return 1;
                }

                if (gdprText.Contains("2"))
                {
                    return 2;
                }
            }

            return 0;
        }

        public static void SetGdprCookie(this HttpResponse response, string gdprId)
        {
            if (string.IsNullOrEmpty(gdprId))
            {
                gdprId = "0";
            }

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddYears(1);
            option.Domain = ".kinauna.com";
            option.IsEssential = true;
            option.Secure = true;
            option.SameSite = SameSiteMode.Lax;
#if DEBUG
            option.Domain = "";
#endif
            response.Cookies.Append("cookieconsent", gdprId, option);
        }
    }
}
