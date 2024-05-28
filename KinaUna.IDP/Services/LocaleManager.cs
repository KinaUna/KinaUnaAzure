using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.IDP.Models.HomeViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUna.IDP.Services
{
    public class LocaleManager : ILocaleManager
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(1));

        public LocaleManager(HttpClient httpClient, IConfiguration configuration, IDistributedCache cache)
        {
            _cache = cache;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
        }

        public async Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId)
        {
            SetLanguageIdViewModel languageIdModel = new()
            {
                LanguageList = await GetAllLanguages(),
                SelectedId = currentLanguageId
            };

            return languageIdModel;

        }
        
        public async Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }

            string translation = "";
            List<TextTranslation> translationsList;
            string cachedTranslationsList = await _cache.GetStringAsync("PageTranslations" + page + "&Lang" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedTranslationsList))
            {
                translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(cachedTranslationsList);
                if (translationsList != null && translationsList.Count != 0)
                {
                    TextTranslation textTranslation = translationsList.FirstOrDefault(t => t.Word == word && t.Page == page && t.LanguageId == languageId);
                    if (textTranslation != null)
                    {
                        translation = textTranslation.Translation;
                    }
                }
            }
            else
            {
                string translationsApiPath = "/api/Translations/PageTranslations/" + languageId + "/" + page;
                HttpResponseMessage translationResponse = await _httpClient.GetAsync(translationsApiPath);

                if (translationResponse.IsSuccessStatusCode)
                {
                    string translationsListAsString = await translationResponse.Content.ReadAsStringAsync();
                    translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(translationsListAsString);

                    if (translationsList != null && translationsList.Count != 0)
                    {
                        await _cache.SetStringAsync("PageTranslations" + page + "&Lang" + languageId, JsonConvert.SerializeObject(translationsList), _cacheExpirationLong);
                        TextTranslation textTranslation = translationsList.FirstOrDefault(t => t.Word == word && t.Page == page && t.LanguageId == languageId);
                        if (textTranslation != null)
                        {
                            translation = textTranslation.Translation;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(translation)) return translation;

            TextTranslation translationItem = new()
            {
                LanguageId = languageId,
                Translation = word,
                Page = page,
                Word = word
            };
            translationItem = await AddTranslation(translationItem);
            translation = translationItem.Translation;

            return translation;
        }

        private async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = [];
            string cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(cachedLanguagesString);
                return languageList;
            }

            const string getAllLanguagesPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(getAllLanguagesPath);

            if (admininfoResponse.IsSuccessStatusCode)
            {
                string languageListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
            }

            if (languageList != null && languageList.Count != 0)
            {
                await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
            }

            return languageList;
        }

        private async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            TextTranslation addedTranslation = new();
            const string addTranslationApiPath = "/api/Translations/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addTranslationApiPath, new StringContent(JsonConvert.SerializeObject(translation), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedTranslation;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedTranslation = JsonConvert.DeserializeObject<TextTranslation>(addResponseString);

            List<KinaUnaLanguage> languages = await GetAllLanguages();
            foreach (KinaUnaLanguage language in languages)
            {
                await _cache.RemoveAsync("PageTranslations" + translation.Page + "&Lang" + language.Id);
            }

            return addedTranslation;
        }

        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText text = new();
            string pageTextsApiPath = "/api/PageTexts/ByTitle/" + title + "/" + page + "/" + languageId;
            HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

            if (!pageTextsResponse.IsSuccessStatusCode) return text;

            string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
            text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);

            return text;
        }

        public int GetLanguageId(HttpRequest request)
        {
            return request.GetLanguageIdFromCookie();
        }
    }
}
