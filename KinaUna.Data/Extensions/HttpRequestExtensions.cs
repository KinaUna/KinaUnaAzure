using System;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for HttpRequest objects.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Gets the language id from the request.
        /// First the method checks if language is set in a cookie.
        /// If not, it tries to get the language from the browser settings.
        /// If no language is found, it defaults to language id 1 = English.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>int with the id of the language</returns>
        public static int GetLanguageIdFromCookie(this HttpRequest request)
        {
            int languageId;
            if (request.Cookies.TryGetValue("languageId", out string languageIdText))
            {
                if (!int.TryParse(languageIdText, out languageId))
                {
                    languageId = request.GetLanguageIdFromBrowser();
                }
            }
            else
            {
                languageId = request.GetLanguageIdFromBrowser();
            }

            if (languageId == 0)
            {
                languageId = 1;
            }

            return languageId;
        }

        /// <summary>
        /// Sets the language id in the languageId cookie.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="languageId"></param>
        public static void SetLanguageCookie(this HttpResponse response, string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                languageId = "1";
            }
            CookieOptions option = new()
            {
                Expires = DateTime.Now.AddYears(1),
                Domain = ".kinauna.com",
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            };
#if DEBUG
            option.Domain = "";
            #endif
            response.Cookies.Append("languageId", languageId, option);  
        }

        /// <summary>
        /// Obtains the language id from the browser settings.
        /// Tries to match the language to the supported languages.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>int with the id of the language</returns>
        private static int GetLanguageIdFromBrowser(this HttpRequest request)
        {
            int languageId = 1;

            try
            {
                request.Headers.TryGetValue("Accept-Language", out StringValues headerLanguages);
                string[] userLanguages = [.. headerLanguages];
                
                if (userLanguages.Length != 0)
                {
                    // Todo: Add dynamic detection of supported languages.
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

        /// <summary>
        /// Determines if the user has accepted the cookie consent.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>bool: True if consent has been accepted.</returns>
        public static bool ConsentCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !string.IsNullOrEmpty(gdprText);
        }

        /// <summary>
        /// Determines if the user has accepted the cookie consent for HereMaps.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>bool: True if the HereMaps cookies have been accepted.</returns>
        public static bool HereMapsCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !(string.IsNullOrEmpty(gdprText)) && gdprText.Contains("heremaps", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Determines if the user has accepted the cookie consent for YouTube.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>bool: True if the YouTube cookies have been accepted.</returns>
        public static bool YouTubeCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !string.IsNullOrEmpty(gdprText) && gdprText.Contains("youtube", StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Retrieves the users preferred language from the request.
        /// First checks if the language is set in the query string.
        /// Then checks if the language is set in a cookie.
        /// Then it tries to get the language from the browser settings.
        /// Defaults to English if no language is found.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>KinaUnaLanguage: The object representing the language to use</returns>
        public static KinaUnaLanguage GetKinaUnaLanguage(this HttpRequest request)
        {
            KinaUnaLanguage kinaUnaLanguage = new()
            {
                Id = 1,
                Code = "en"
            };

            if (request.Query.ContainsKey("languageId"))
            {
                if (int.TryParse(request.Query["languageId"], out int queryValue))
                {
                    kinaUnaLanguage.Id = queryValue;
                }
            }
            else
            {
                if (request.Cookies.TryGetValue("languageId", out string languageIdText))
                {
                    if (!int.TryParse(languageIdText, out int languageId))
                    {
                        kinaUnaLanguage.Id = 1;
                    }
                    else
                    {
                        kinaUnaLanguage.Id = languageId;
                    }
                }
                else
                {
                    try
                    {
                        string[] userLanguages = [.. request.GetTypedHeaders()
                            .AcceptLanguage
                            .OrderByDescending(x => x.Quality ?? 1)
                            .Select(x => x.Value.ToString())];

                        string firstLang = userLanguages.FirstOrDefault();

                        if (firstLang != null && firstLang.StartsWith("de"))
                        {
                            kinaUnaLanguage.Id = 2;
                        }

                        if (firstLang != null && firstLang.StartsWith("da"))
                        {
                            kinaUnaLanguage.Id = 3;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        kinaUnaLanguage.Id = 1;
                    }
                }
            }

            if (kinaUnaLanguage.Id == 1)
            {
                kinaUnaLanguage.Code = "en";
            }

            if (kinaUnaLanguage.Id == 2)
            {
                kinaUnaLanguage.Code = "de";
            }

            if (kinaUnaLanguage.Id == 3)
            {
                kinaUnaLanguage.Code = "da";
            }

            return kinaUnaLanguage;
        }
    }
}
