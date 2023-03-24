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
            CookieOptions option = new CookieOptions
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
                string[] userLanguages = headerLanguages.ToArray();// .OrderByDescending(x => x.Quality ?? 1).Select(x => x.Value.ToString()).ToArray();
                
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


        public static bool ConsentCookieSet(this HttpRequest request)
        {
            if (request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText))
            {
                if (string.IsNullOrEmpty(gdprText))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool HereMapsCookieSet(this HttpRequest request)
        {
            if (request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText))
            {
                if (!(string.IsNullOrEmpty(gdprText)) && gdprText.ToLower().Contains("heremaps"))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool YouTubeCookieSet(this HttpRequest request)
        {
            if (request.Cookies.TryGetValue("KinaUnaConsent", out string gdprText))
            {
                if (!(string.IsNullOrEmpty(gdprText)) && gdprText.ToLower().Contains("youtube"))
                {
                    return true;
                }
            }

            return false;
        }

        public static KinaUnaLanguage GetKinaUnaLanguage(this HttpRequest request)
        {
            KinaUnaLanguage kinaUnaLanguage = new KinaUnaLanguage
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
