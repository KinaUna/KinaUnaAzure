using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class PageTextsHttpClient:IPageTextsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(20));

        public PageTextsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IDistributedCache cache)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _cache = cache;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        
        private async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = new();
            string cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(cachedLanguagesString);
                return languageList;
            }

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
            _httpClient.SetBearerToken(accessToken);

            string languageApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage languageResponse = await _httpClient.GetAsync(languageApiPath);
            
            if (languageResponse.IsSuccessStatusCode)
            {
                string languageListAsString = await languageResponse.Content.ReadAsStringAsync();
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
                if (languageList != null && languageList.Any())
                {
                    await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
                }
            }
            
            return languageList;
        }

        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId, bool updateCache = false)
        {
            KinaUnaText text = new();
            string cachedText = await _cache.GetStringAsync("PageText" + title + "&Page" + page + "&Lang" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/ByTitle/" + title + "/" + page + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);
                    if (text == null || text.Id == 0)
                    {
                        KinaUnaText newKinaUnaText = new()
                        {
                            Title = title,
                            Page = page,
                            LanguageId = languageId,
                            Text = ""
                        };
                        text = await AddPageText(newKinaUnaText);
                    }

                    if (text != null)
                    {
                        await _cache.SetStringAsync("PageText" + title + "&Page" + page + "&Lang" + languageId, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                    }
                }
            }
            
            return text;
        }

        public async Task<KinaUnaText> AddPageText(KinaUnaText textItem)
        {
            KinaUnaText addedTextItem = new();
            textItem.Text ??= "";

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/PageTexts/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(textItem), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                string addResponseString = await addResponse.Content.ReadAsStringAsync();
                addedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(addResponseString);
                if (addedTextItem != null)
                {
                    List<KinaUnaLanguage> languages = await GetAllLanguages();
                    foreach (KinaUnaLanguage language in languages)
                    {
                        KinaUnaText pText = await GetPageTextByTextId(addedTextItem.TextId, language.Id, true);
                        await _cache.RemoveAsync("PageText&TextId" + pText.TextId + "&Language" + language.Id);
                        await _cache.RemoveAsync("PageText" + pText.Title + "&Page" + addedTextItem.Page + "&Lang" + language.Id);
                        await _cache.RemoveAsync("PageTextsForPage" + addedTextItem.Page + "&Lang" + language.Id);
                    }
                }
            }
            
            return addedTextItem;
        }

        public async Task<KinaUnaText> GetPageTextById(int id, bool updateCache = false)
        {
            KinaUnaText text = new();
            string cachedText = await _cache.GetStringAsync("PageText&Id" + id);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextById/" + id;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);
                    await _cache.SetStringAsync("PageText&Id" + id, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                }
            }
            
            return text;
        }

        public async Task<KinaUnaText> GetPageTextByTextId(int textId, int languageId, bool updateCache = false)
        {
            KinaUnaText text = new();
            string cachedText = await _cache.GetStringAsync("PageText&TextId" + textId + "&Language" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextByTextId/" + textId + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);
                    await _cache.SetStringAsync("PageText&TextId" + textId + "&Language" + languageId, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                }
            }

            return text;
        }

        public async Task<KinaUnaText> UpdatePageText(KinaUnaText kinaUnaText)
        {
            KinaUnaText updatedTextItem = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string textsApiPath = "/api/PageTexts/" + kinaUnaText.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(textsApiPath, new StringContent(JsonConvert.SerializeObject(kinaUnaText), System.Text.Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode)
            {
                string updateResponseString = await updateResponse.Content.ReadAsStringAsync();
                updatedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(updateResponseString);
            }
            
            return updatedTextItem;
        }

        public async Task<List<KinaUnaText>> GetAllKinaUnaTexts(int languageId = 0, bool updateCache = false)
        {
            List<KinaUnaText> allKinaUnaTexts = new();
            string cachedTextsList = await _cache.GetStringAsync("AllKinaUnaTexts" + "&Lang" + languageId);
            if (!updateCache && languageId != 0 && !string.IsNullOrEmpty(cachedTextsList))
            {
                allKinaUnaTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(cachedTextsList);
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string admininfoApiPath = "/api/PageTexts/GetAllTexts/" + languageId;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (admininfoResponse.IsSuccessStatusCode)
                {
                    string textsListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                    allKinaUnaTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(textsListAsString);
                    await _cache.SetStringAsync("AllKinaUnaTexts" + "&Lang" + languageId, JsonConvert.SerializeObject(allKinaUnaTexts), _cacheExpirationLong);
                }
            }

            return allKinaUnaTexts;
        }
    }
}
