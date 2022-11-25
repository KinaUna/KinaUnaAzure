using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class PageTextsHttpClient:IPageTextsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(20));

        public PageTextsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env, IDistributedCache cache)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _env = env;
            _cache = cache;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer) ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext? currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer) ??
                                               throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");
            }

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration"));
            return accessToken;
        }

        private async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = new List<KinaUnaLanguage>();
            string? cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(cachedLanguagesString);
                return languageList;
            }

            string accessToken = await GetNewToken(true);
            _httpClient.SetBearerToken(accessToken);

            string languageApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage languageResponse = await _httpClient.GetAsync(languageApiPath);
            
            if (languageResponse.IsSuccessStatusCode)
            {
                string languageListAsString = await languageResponse.Content.ReadAsStringAsync();
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
                if (languageList.Any())
                {
                    await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
                }
            }
            
            return languageList;
        }

        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId, bool updateCache = false)
        {
            KinaUnaText text = new KinaUnaText();
            string? cachedText = await _cache.GetStringAsync("PageText" + title + "&Page" + page + "&Lang" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await GetNewToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/ByTitle/" + title + "/" + page + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string pivoqTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(pivoqTextAsString);
                    if (text.Id == 0)
                    {
                        KinaUnaText newPivoqText = new KinaUnaText();
                        newPivoqText.Title = title;
                        newPivoqText.Page = page;
                        newPivoqText.LanguageId = languageId;
                        newPivoqText.Text = "";
                        text = await AddPageText(newPivoqText);
                    }

                    await _cache.SetStringAsync("PageText" + title + "&Page" + page + "&Lang" + languageId, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                }
            }
            
            return text;
        }

        public async Task<KinaUnaText> AddPageText(KinaUnaText textItem)
        {
            KinaUnaText addedTextItem = new KinaUnaText();
            textItem.Text ??= "";

            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/PageTexts/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(textItem), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                string addResponseString = await addResponse.Content.ReadAsStringAsync();
                addedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(addResponseString);
                List<KinaUnaLanguage> languages = await GetAllLanguages();
                foreach (KinaUnaLanguage language in languages)
                {
                    KinaUnaText pText = await GetPageTextByTextId(addedTextItem.TextId, language.Id, true);
                    await _cache.RemoveAsync("PageText&TextId" + pText.TextId + "&Language" + language.Id);
                    await _cache.RemoveAsync("PageText" + pText.Title + "&Page" + addedTextItem.Page + "&Lang" + language.Id);
                    await _cache.RemoveAsync("PageTextsForPage" + addedTextItem.Page + "&Lang" + language.Id);
                }
            }
            
            return addedTextItem;
        }

        public async Task<KinaUnaText> GetPageTextById(int id, bool updateCache = false)
        {
            KinaUnaText text = new KinaUnaText();
            string? cachedText = await _cache.GetStringAsync("PageText&Id" + id);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await GetNewToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextById/" + id;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string pivoqTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(pivoqTextAsString);
                    await _cache.SetStringAsync("PageText&Id" + id, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                }
            }
            
            return text;
        }

        public async Task<KinaUnaText> GetPageTextByTextId(int textId, int languageId, bool updateCache = false)
        {
            KinaUnaText text = new KinaUnaText();
            string? cachedText = await _cache.GetStringAsync("PageText&TextId" + textId + "&Language" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedText))
            {
                text = JsonConvert.DeserializeObject<KinaUnaText>(cachedText);
            }
            else
            {
                string accessToken = await GetNewToken(true);
                _httpClient.SetBearerToken(accessToken);

                string pageTextsApiPath = "/api/PageTexts/GetTextByTextId/" + textId + "/" + languageId;
                HttpResponseMessage pageTextsResponse = await _httpClient.GetAsync(pageTextsApiPath);

                if (pageTextsResponse.IsSuccessStatusCode)
                {
                    string pivoqTextAsString = await pageTextsResponse.Content.ReadAsStringAsync();
                    text = JsonConvert.DeserializeObject<KinaUnaText>(pivoqTextAsString);
                    await _cache.SetStringAsync("PageText&TextId" + textId + "&Language" + languageId, JsonConvert.SerializeObject(text), _cacheExpirationLong);
                }
            }

            return text;
        }

        public async Task<KinaUnaText> UpdatePageText(KinaUnaText pivoqText)
        {
            KinaUnaText updatedTextItem = new KinaUnaText();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string textsApiPath = "/api/PageTexts/" + pivoqText.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(textsApiPath, new StringContent(JsonConvert.SerializeObject(pivoqText), System.Text.Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode)
            {
                string updateResponseString = await updateResponse.Content.ReadAsStringAsync();
                updatedTextItem = JsonConvert.DeserializeObject<KinaUnaText>(updateResponseString);
            }
            
            return updatedTextItem;
        }

        public async Task<List<KinaUnaText>> GetAllPivoqTexts(int languageId = 0, bool updateCache = false)
        {
            List<KinaUnaText> allPivoqTexts = new List<KinaUnaText>();
            string? cachedTextsList = await _cache.GetStringAsync("AllPivoqTexts" + "&Lang" + languageId);
            if (!updateCache && languageId != 0 && !string.IsNullOrEmpty(cachedTextsList))
            {
                allPivoqTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(cachedTextsList);
            }
            else
            {
                string accessToken = await GetNewToken(true);
                _httpClient.SetBearerToken(accessToken);

                string admininfoApiPath = "/api/PageTexts/GetAllTexts/" + languageId;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (admininfoResponse.IsSuccessStatusCode)
                {
                    string textsListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                    allPivoqTexts = JsonConvert.DeserializeObject<List<KinaUnaText>>(textsListAsString);
                    await _cache.SetStringAsync("AllPivoqTexts" + "&Lang" + languageId, JsonConvert.SerializeObject(allPivoqTexts), _cacheExpirationLong);
                }
            }

            return allPivoqTexts;
        }
    }
}
