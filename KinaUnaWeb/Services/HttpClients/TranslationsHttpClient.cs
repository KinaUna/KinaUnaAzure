using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Translations API.
    /// Contains the methods for adding, retrieving and updating data relevant to translation functions.
    /// </summary>
    public class TranslationsHttpClient : ITranslationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(21));

        public TranslationsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IDistributedCache cache, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
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

            const string admininfoApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

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

        /// <summary>
        /// Gets a translation for a given word (or phrase) and page translated into a given language.
        /// If the TextTranslation doesn't exist, it is added to the database.
        /// </summary>
        /// <param name="word">The Word property of the translation to get (usually the English version).</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The language it should be translated to.</param>
        /// <param name="updateCache">Force update the cache. False: Attempt to get the translation from the cache. True: Get the translation from the API and update the cache.</param>
        /// <returns>String with the translated word or phrase.</returns>
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
                if (translationsList != null)
                {
                    translation = translationsList.FirstOrDefault(t => t.LanguageId == languageId && t.Word == word)?.Translation;
                }
            }
            else
            {
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string admininfoApiPath = "/api/Translations/PageTranslations/" + languageId + "/" + page;
                HttpResponseMessage translationResponse = await _httpClient.GetAsync(admininfoApiPath);

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

        /// <summary>
        /// Adds a new TextTranslation to the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object to add.</param>
        /// <returns>The added TextTranslation object.If an error happens a new TextTranslation with Id=0 is returned.</returns>
        public async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            TextTranslation addedTranslation = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string addApiPath = "/api/Translations/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(translation), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedTranslation;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedTranslation = JsonConvert.DeserializeObject<TextTranslation>(addResponseString);

            List<KinaUnaLanguage> languages = await GetAllLanguages();
            foreach (KinaUnaLanguage language in languages)
            {
                await _cache.RemoveAsync("PageTranslations" + translation.Page + "&Lang" + language.Id);
                _ = await GetAllTranslations(language.Id, true);
            }

            return addedTranslation;
        }

        /// <summary>
        /// Updates a TextTranslation in the database.
        /// </summary>
        /// <param name="translation">The TextTranslation object with the updated properties.</param>
        /// <returns>The updated TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        public async Task<TextTranslation> UpdateTranslation(TextTranslation translation)
        {
            TextTranslation addedTranslation = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string addApiPath = "/api/Translations/" + translation.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(translation), System.Text.Encoding.UTF8, "application/json"));
            if (!updateResponse.IsSuccessStatusCode) return addedTranslation;

            string addResponseString = await updateResponse.Content.ReadAsStringAsync();
            addedTranslation = JsonConvert.DeserializeObject<TextTranslation>(addResponseString);

            return addedTranslation;
        }

        /// <summary>
        /// Deletes a TextTranslation from the database.
        /// Also deletes the translations in all other languages for the same word and page.
        /// </summary>
        /// <param name="translation">The TextTranslation object to delete.</param>
        /// <returns>The deleted TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        public async Task<TextTranslation> DeleteTranslation(TextTranslation translation)
        {
            TextTranslation deletedTranslation = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string addApiPath = "/api/Translations/" + translation.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(addApiPath);
            if (!deleteResponse.IsSuccessStatusCode) return deletedTranslation;

            string deleteResponseString = await _httpClient.DeleteAsync(addApiPath).Result.Content.ReadAsStringAsync();
            deletedTranslation = JsonConvert.DeserializeObject<TextTranslation>(deleteResponseString);

            return deletedTranslation;
        }

        /// <summary>
        /// Deletes a single translation from the database.
        /// Doesn't delete translations in other languages for the same word and page.
        /// </summary>
        /// <param name="translation">The TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object. If not found or an error happens a new TextTranslation with Id=0 is returned.</returns>
        public async Task<TextTranslation> DeleteSingleItemTranslation(TextTranslation translation)
        {
            TextTranslation deletedTranslation = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string addApiPath = "/api/Translations/DeleteSingleItem/" + translation.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(addApiPath);
            if (!deleteResponse.IsSuccessStatusCode) return deletedTranslation;

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedTranslation = JsonConvert.DeserializeObject<TextTranslation>(deleteResponseString);
            List<KinaUnaLanguage> languages = await GetAllLanguages();
            foreach (KinaUnaLanguage language in languages)
            {
                if (deletedTranslation != null) await _cache.RemoveAsync("PageTranslations" + deletedTranslation.Page + "&Lang" + language.Id);
                _ = await GetAllTranslations(language.Id, true).ConfigureAwait(false);
            }

            return deletedTranslation;
        }

        /// <summary>
        /// Gets the list of all TextTranslations in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the TextTranslations to get.</param>
        /// <param name="updateCache">Force update the cache. If False, tries to get the list from the cache first, if True gets the list from the API first and updates the cache.</param>
        /// <returns>List of TextTranslation objects.</returns>
        public async Task<List<TextTranslation>> GetAllTranslations(int languageId = 0, bool updateCache = false)
        {
            List<TextTranslation> translationsList = [];
            string cachedTranslationsList = await _cache.GetStringAsync("AllTranslations" + "&Lang" + languageId);
            if (!updateCache && languageId != 0 && !string.IsNullOrEmpty(cachedTranslationsList))
            {
                translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(cachedTranslationsList);
            }
            else
            {
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string admininfoApiPath = "/api/Translations/GetAllTranslations/" + languageId;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);
                if (!admininfoResponse.IsSuccessStatusCode) return translationsList;

                string translationsListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(translationsListAsString);
                await _cache.SetStringAsync("AllTranslations" + "&Lang" + languageId, JsonConvert.SerializeObject(translationsList), _cacheExpirationLong);
            }

            return translationsList;
        }

        /// <summary>
        /// Gets a TextTranslation by Id.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to get.</param>
        /// <param name="updateCache">Force update the cache. If False, tries to get the TextTranslation from the cache first, if True gets it from the API first and updates the cache.</param>
        /// <returns>TextTranslation object with the given Id. New TextTranslation object with Id=0 if not found or an error occurs.</returns>
        public async Task<TextTranslation> GetTranslationById(int id, bool updateCache = false)
        {
            TextTranslation textTranslation = new();
            string cachedTranslation = await _cache.GetStringAsync("TranslationById" + id);
            if (!updateCache && !string.IsNullOrEmpty(cachedTranslation))
            {
                textTranslation = JsonConvert.DeserializeObject<TextTranslation>(cachedTranslation);
            }
            else
            {
                string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
                TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
                _httpClient.SetBearerToken(tokenInfo.AccessToken);

                string admininfoApiPath = "/api/Translations/GetTranslationById/" + id;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (!admininfoResponse.IsSuccessStatusCode) return textTranslation;

                string translationAsString = await admininfoResponse.Content.ReadAsStringAsync();
                textTranslation = JsonConvert.DeserializeObject<TextTranslation>(translationAsString);
                await _cache.SetStringAsync("TranslationById" + id, JsonConvert.SerializeObject(textTranslation), _cacheExpirationLong);
            }

            return textTranslation;
        }
    }
}
