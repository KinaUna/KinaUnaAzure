using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the PageTexts API.
    /// Contains the methods for adding, retrieving and updating data relevant to page text functions.
    /// </summary>
    public class PageTextsHttpClient : IPageTextsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(20));

        public PageTextsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IDistributedCache cache, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            _cache = cache;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets the list of all KinaUnaLanguages.
        /// </summary>
        /// <param name="updateCache">If false attempts to get the list from cache first, if true force updates cache.</param>
        /// <returns>List of KinaUnaLanguage objects.</returns>
        private async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = [];
            string cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(cachedLanguagesString);
                return languageList;
            }

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string languageApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage languageResponse = await _httpClient.GetAsync(languageApiPath);

            if (!languageResponse.IsSuccessStatusCode) return languageList;

            string languageListAsString = await languageResponse.Content.ReadAsStringAsync();
            languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
            if (languageList != null && languageList.Count != 0)
            {
                await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
            }

            return languageList;
        }

        /// <summary>
        /// Gets the KinaUnaText with the given Title and Page, translated into the given language.
        /// If the text does not exist, a new KinaUnaText entry is created.
        /// </summary>
        /// <param name="title">The Title property of the KinaUnaText to get.</param>
        /// <param name="page">The name of the page the text appears on.</param>
        /// <param name="languageId">The language to get the text in.</param>
        /// <param name="updateCache">If false, attempts to get the KinaUnaText from the cache first. If true, force updates the cache.</param>
        /// <returns>KinaUnaText object.</returns>
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
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string pageTextsApiPath = "/api/PageTexts/ByTitle/" + title + "/" + page + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (!pageTextsResponse.IsSuccessStatusCode) return text;

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

            return text;
        }

        /// <summary>
        /// Adds a new KinaUnaText entry.
        /// For internal use only when a page text is not found, end users should never use this directly.
        /// </summary>
        /// <param name="textItem">The KinaUnaText object to add.</param>
        /// <returns>The added KinaUnaText object.</returns>
        private async Task<KinaUnaText> AddPageText(KinaUnaText textItem)
        {
            KinaUnaText addedTextItem = new();
            textItem.Text ??= "";

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string addApiPath = "/api/PageTexts/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(textItem), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedTextItem;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(addResponseString);
            if (addedTextItem == null) return null;

            List<KinaUnaLanguage> languages = await GetAllLanguages();
            foreach (KinaUnaLanguage language in languages)
            {
                KinaUnaText pText = await GetPageTextByTextId(addedTextItem.TextId, language.Id, true);
                await _cache.RemoveAsync("PageText&TextId" + pText.TextId + "&Language" + language.Id);
                await _cache.RemoveAsync("PageText" + pText.Title + "&Page" + addedTextItem.Page + "&Lang" + language.Id);
                await _cache.RemoveAsync("PageTextsForPage" + addedTextItem.Page + "&Lang" + language.Id);
            }

            return addedTextItem;
        }

        /// <summary>
        /// Gets the KinaUnaText with the given Id.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to get.</param>
        /// <param name="updateCache">If False, attempts to get the item from cache first. If True, gets the item from the API and updates the cache.</param>
        /// <returns>The KinaUnaText object with the given Id.</returns>
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
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextById/" + id;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (!pageTextsResponse.IsSuccessStatusCode) return text;

                string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);
                await _cache.SetStringAsync("PageText&Id" + id, JsonConvert.SerializeObject(text), _cacheExpirationLong);
            }

            return text;
        }

        /// <summary>
        /// Gets the KinaUnaText with the given TextId, translated into the language with the given LanguageId.
        /// </summary>
        /// <param name="textId">The TextId of the KinaUnaText to get.</param>
        /// <param name="languageId">The LanguageId of the language to display the text in.</param>
        /// <param name="updateCache">If False, attempts to get the item from cache first. If True, gets the item from the API and updates the cache.</param>
        /// <returns>The KinaUnaText object with the given TextId and LanguageId.</returns>
        private async Task<KinaUnaText> GetPageTextByTextId(int textId, int languageId, bool updateCache = false)
        {
            KinaUnaText text = new();
            string cachedText = await _cache.GetStringAsync("PageText&TextId" + textId + "&Language" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextByTextId/" + textId + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (!pageTextsResponse.IsSuccessStatusCode) return text;

                string kinaUnaTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                text = JsonConvert.DeserializeObject<KinaUnaText>(kinaUnaTextAsString);
                await _cache.SetStringAsync("PageText&TextId" + textId + "&Language" + languageId, JsonConvert.SerializeObject(text), _cacheExpirationLong);
            }

            return text;
        }

        /// <summary>
        /// Updates a KinaUnaText entry.
        /// </summary>
        /// <param name="kinaUnaText">The KinaUnaText object with the updated properties.</param>
        /// <returns>The updated KinaUnaText object.</returns>
        public async Task<KinaUnaText> UpdatePageText(KinaUnaText kinaUnaText)
        {
            KinaUnaText updatedTextItem = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string textsApiPath = "/api/PageTexts/" + kinaUnaText.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(textsApiPath, new StringContent(JsonConvert.SerializeObject(kinaUnaText), System.Text.Encoding.UTF8, "application/json"));
            if (!updateResponse.IsSuccessStatusCode) return updatedTextItem;

            string updateResponseString = await updateResponse.Content.ReadAsStringAsync();
            updatedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(updateResponseString);

            return updatedTextItem;
        }

        /// <summary>
        /// Gets the list of all KinaUnaTexts in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the KinaUnaTexts to get.</param>
        /// <param name="updateCache">If False, attempts to get the ist from cache first. If True, gets the list from the API and updates the cache.</param>
        /// <returns>List of KinaUnaText objects.</returns>
        public async Task<List<KinaUnaText>> GetAllKinaUnaTexts(int languageId = 0, bool updateCache = false)
        {
            List<KinaUnaText> allKinaUnaTexts = [];
            string cachedTextsList = await _cache.GetStringAsync("AllKinaUnaTexts" + "&Lang" + languageId);
            if (!updateCache && languageId != 0 && !string.IsNullOrEmpty(cachedTextsList))
            {
                allKinaUnaTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(cachedTextsList);
            }
            else
            {
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string admininfoApiPath = "/api/PageTexts/GetAllTexts/" + languageId;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);
                if (!admininfoResponse.IsSuccessStatusCode) return allKinaUnaTexts;

                string textsListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                allKinaUnaTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(textsListAsString);
                await _cache.SetStringAsync("AllKinaUnaTexts" + "&Lang" + languageId, JsonConvert.SerializeObject(allKinaUnaTexts), _cacheExpirationLong);
            }

            return allKinaUnaTexts;
        }
    }
}
