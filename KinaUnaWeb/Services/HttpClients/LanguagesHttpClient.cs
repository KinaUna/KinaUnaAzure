using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Languages API.
    /// </summary>
    public class LanguagesHttpClient : ILanguagesHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(22));

        public LanguagesHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ITokenService tokenService, IDistributedCache cache, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            _cache = cache;
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
        }

        /// <summary>
        /// Gets the list of all KinaUnaLanguages from the cache.
        /// If the list is not found in the cache, or if updateCache is true, the list is fetched from the API.
        /// </summary>
        /// <param name="updateCache">Get the list from the API first and update the cache.</param>
        /// <returns>List of KinaUnaLanguage objects.</returns>
        public async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = [];
            string cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonSerializer.Deserialize<List<KinaUnaLanguage>>(cachedLanguagesString, JsonSerializerOptions.Web);
                return languageList;
            }

            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(string.Empty);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string admininfoApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (!admininfoResponse.IsSuccessStatusCode) return languageList;

            string languageListAsString = await admininfoResponse.Content.ReadAsStringAsync();
            languageList = JsonSerializer.Deserialize<List<KinaUnaLanguage>>(languageListAsString, JsonSerializerOptions.Web);
            if (languageList != null && languageList.Count != 0)
            {
                await _cache.SetStringAsync("AllLanguages", JsonSerializer.Serialize(languageList, JsonSerializerOptions.Web));
            }

            return languageList;
        }

        /// <summary>
        /// Gets the KinaUnaLanguage with the given Id.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage to get.</param>
        /// <param name="updateCache">Get the KinaUnaLanguage from the API first and update the cache.</param>
        /// <returns>KinaUnaLanguage object with the given Id.</returns>
        public async Task<KinaUnaLanguage> GetLanguage(int languageId, bool updateCache = false)
        {
            KinaUnaLanguage language = new();
            string cachedLanguageString = await _cache.GetStringAsync("Language" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguageString))
            {
                language = JsonSerializer.Deserialize<KinaUnaLanguage>(cachedLanguageString, JsonSerializerOptions.Web);
                return language;
            }

            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(string.Empty);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string admininfoApiPath = "/api/Languages/GetLanguage/" + languageId;
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (!admininfoResponse.IsSuccessStatusCode) return language;

            string languageAsString = await admininfoResponse.Content.ReadAsStringAsync();
            language = JsonSerializer.Deserialize<KinaUnaLanguage>(languageAsString, JsonSerializerOptions.Web);
            await _cache.SetStringAsync("Language" + languageId, JsonSerializer.Serialize(language, JsonSerializerOptions.Web), _cacheExpirationLong);

            return language;
        }

        /// <summary>
        /// Adds a new KinaUnaLanguage.
        /// Only KinaUnaAdmins can add new languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object to add.</param>
        /// <returns>The added KinaUnaLanguage object.</returns>
        public async Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage addedLanguage = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string addApiPath = "/api/Languages/AddLanguage/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonSerializer.Serialize(language, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedLanguage;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedLanguage = JsonSerializer.Deserialize<KinaUnaLanguage>(addResponseString, JsonSerializerOptions.Web);

            return addedLanguage;
        }

        /// <summary>
        /// Updates a KinaUnaLanguage.
        /// Only KinaUnaAdmins can update languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object with the updated properties.</param>
        /// <returns>The updated KinaUnaLanguage object.</returns>
        public async Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage updatedLanguage = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateApiPath = "/api/Languages/UpdateLanguage/" + language.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonSerializer.Serialize(language, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!updateResponse.IsSuccessStatusCode) return updatedLanguage;

            string updateResponseString = await updateResponse.Content.ReadAsStringAsync();
            updatedLanguage = JsonSerializer.Deserialize<KinaUnaLanguage>(updateResponseString, JsonSerializerOptions.Web);

            return updatedLanguage;
        }

        /// <summary>
        /// Deletes a KinaUnaLanguage and removes it from the cache.
        /// Also removes the list of all languages from the cache, forcing a cache update.
        /// Only KinaUnaAdmins can delete languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object to delete.</param>
        /// <returns>The deleted KinaUnaLanguage object.</returns>
        public async Task<KinaUnaLanguage> DeleteLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage deletedLanguage = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string deleteApiPath = "/api/Languages/DeleteLanguage/" + language.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(deleteApiPath);
            if (!deleteResponse.IsSuccessStatusCode) return deletedLanguage;

            string deletedResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedLanguage = JsonSerializer.Deserialize<KinaUnaLanguage>(deletedResponseString, JsonSerializerOptions.Web);
            await _cache.RemoveAsync("Language" + language.Id);
            await _cache.RemoveAsync("AllLanguages");

            return deletedLanguage;
        }
    }
}
