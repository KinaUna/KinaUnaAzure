using System;
using System.Linq;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace KinaUna.Data.Extensions
{
    public static class HttpRequestExtensions
    {
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

        private static int GetLanguageIdFromBrowser(this HttpRequest request)
        {
            int languageId = 1;

            try
            {
                request.Headers.TryGetValue("Accept-Language", out StringValues headerLanguages);
                string[] userLanguages = [.. headerLanguages];
                
                if (userLanguages.Length != 0)
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


        public static bool ConsentCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !string.IsNullOrEmpty(gdprText);
        }

        public static bool HereMapsCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !(string.IsNullOrEmpty(gdprText)) && gdprText.Contains("heremaps", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool YouTubeCookieSet(this HttpRequest request)
        {
            if (!request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText)) return false;

            return !string.IsNullOrEmpty(gdprText) && gdprText.Contains("youtube", StringComparison.CurrentCultureIgnoreCase);
        }

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
                        string[] userLanguages = request.GetTypedHeaders().AcceptLanguage.OrderByDescending(x => x.Quality ?? 1).Select(x => x.Value.ToString()).ToArray();
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
