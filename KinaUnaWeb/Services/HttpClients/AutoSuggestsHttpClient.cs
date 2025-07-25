using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the AutoSuggests API.
    /// </summary>
    public class AutoSuggestsHttpClient : IAutoSuggestsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;

        public AutoSuggestsHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            string clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey);
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Local");
            }

            if (env.IsStaging())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Azure");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _tokenService = tokenService;
        }

        /// <summary>
        /// Gets the list of all unique tags for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get tags for.</param>
        /// <returns>List of strings.</returns>
        public async Task<List<string>> GetTagsList(int progenyId)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(userId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<string> resultTagsList = [];

            string tagsApiPath = "/api/AutoSuggests/GetTagsAutoSuggestList/" + progenyId;

            HttpResponseMessage tagsResponse = await _httpClient.GetAsync(tagsApiPath).ConfigureAwait(false);
            if (!tagsResponse.IsSuccessStatusCode) return resultTagsList;

            string tagsListAsString = await tagsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultTagsList = JsonConvert.DeserializeObject<List<string>>(tagsListAsString);

            return resultTagsList;
        }

        /// <summary>
        /// Gets the list of all unique contexts for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get contexts for.</param>
        /// <returns>List of strings.</returns>
        public async Task<List<string>> GetContextsList(int progenyId)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(userId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<string> resultContextsList = [];

            string contextsApiPath = "/api/AutoSuggests/GetContextAutoSuggestList/" + progenyId;

            HttpResponseMessage contextsResponse = await _httpClient.GetAsync(contextsApiPath).ConfigureAwait(false);
            if (!contextsResponse.IsSuccessStatusCode) return resultContextsList;

            string contextsListAsString = await contextsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultContextsList = JsonConvert.DeserializeObject<List<string>>(contextsListAsString);

            return resultContextsList;
        }

        /// <summary>
        /// Gets the list of all unique location names for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get locations for.</param>
        /// <returns>List of strings.</returns>
        public async Task<List<string>> GetLocationsList(int progenyId)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(userId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<string> resultLocationsList = [];

            string locationsApiPath = "/api/AutoSuggests/GetLocationAutoSuggestList/" + progenyId;

            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (!locationsResponse.IsSuccessStatusCode) return resultLocationsList;

            string locationsListAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultLocationsList = JsonConvert.DeserializeObject<List<string>>(locationsListAsString);

            return resultLocationsList;
        }

        /// <summary>
        /// Gets the list of all unique categories for a Progeny, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get categories for.</param>
        /// <returns>List of strings.</returns>
        public async Task<List<string>> GetCategoriesList(int progenyId)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(userId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<string> resultCategoriesList = [];

            string categoriesApiPath = "/api/AutoSuggests/GetCategoryAutoSuggestList/" + progenyId;

            HttpResponseMessage categoriesResponse = await _httpClient.GetAsync(categoriesApiPath).ConfigureAwait(false);
            if (!categoriesResponse.IsSuccessStatusCode) return resultCategoriesList;

            string categoriesListAsString = await categoriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultCategoriesList = JsonConvert.DeserializeObject<List<string>>(categoriesListAsString);

            return resultCategoriesList;
        }

        /// <summary>
        /// Gets the list of all unique languages for a Progeny's VocabularyItems, including only items that the user has access to.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get languages for.</param>
        /// <returns>List of strings.</returns>
        public async Task<List<string>> GetVocabularyLanguageList(int progenyId)
        {
            string userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(userId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<string> resultLanguagesList = [];

            string languagesApiPath = "/api/AutoSuggests/GetVocabularyLanguagesSuggestList/" + progenyId;

            HttpResponseMessage languagesResponse = await _httpClient.GetAsync(languagesApiPath).ConfigureAwait(false);
            if (!languagesResponse.IsSuccessStatusCode) return resultLanguagesList;

            string languagesListAsString = await languagesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultLanguagesList = JsonConvert.DeserializeObject<List<string>>(languagesListAsString);

            return resultLanguagesList;
        }
    }
}
