﻿using System;
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

namespace KinaUnaWeb.Services.HttpClients
{
    public class TranslationsHttpClient : ITranslationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(21));

        public TranslationsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IDistributedCache cache)
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

            string admininfoApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (admininfoResponse.IsSuccessStatusCode)
            {
                string languageListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
            }

            if (languageList != null && languageList.Any())
            {
                await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
            }

            return languageList;
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
                if (translationsList != null)
                {
                    translation = translationsList.FirstOrDefault(t => t.LanguageId == languageId && t.Word == word)?.Translation;
                }
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string admininfoApiPath = "/api/Translations/PageTranslations/" + languageId + "/" + page;
                HttpResponseMessage translationResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (translationResponse.IsSuccessStatusCode)
                {
                    string translationsListAsString = await translationResponse.Content.ReadAsStringAsync();
                    translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(translationsListAsString);

                    if (translationsList != null && translationsList.Any())
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

            if (string.IsNullOrEmpty(translation))
            {
                TextTranslation translationItem = new()
                {
                    LanguageId = languageId,
                    Translation = word,
                    Page = page,
                    Word = word
                };
                translationItem = await AddTranslation(translationItem);
                translation = translationItem.Translation;
            }
            return translation;

        }

        public async Task<TextTranslation> AddTranslation(TextTranslation translation)
        {
            TextTranslation addedTranslation = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Translations/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(translation), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                string addResponseString = await addResponse.Content.ReadAsStringAsync();
                addedTranslation = JsonConvert.DeserializeObject<TextTranslation>(addResponseString);

                List<KinaUnaLanguage> languages = await GetAllLanguages();
                foreach (KinaUnaLanguage language in languages)
                {
                    await _cache.RemoveAsync("PageTranslations" + translation.Page + "&Lang" + language.Id);
                    _ = await GetAllTranslations(language.Id, true);
                }
            }


            return addedTranslation;
        }

        public async Task<TextTranslation> UpdateTranslation(TextTranslation translation)
        {
            TextTranslation addedTranslation = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Translations/" + translation.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(translation), System.Text.Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode)
            {
                string addResponseString = await updateResponse.Content.ReadAsStringAsync();
                addedTranslation = JsonConvert.DeserializeObject<TextTranslation>(addResponseString);
            }

            return addedTranslation;
        }

        public async Task<TextTranslation> DeleteTranslation(TextTranslation translation)
        {
            TextTranslation deletedTranslation = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Translations/" + translation.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(addApiPath);
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deleteResponseString = await _httpClient.DeleteAsync(addApiPath).Result.Content.ReadAsStringAsync();
                deletedTranslation = JsonConvert.DeserializeObject<TextTranslation>(deleteResponseString);
            }


            return deletedTranslation;
        }

        public async Task<TextTranslation> DeleteSingleItemTranslation(TextTranslation translation)
        {
            TextTranslation deletedTranslation = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Translations/DeleteSingleItem/" + translation.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(addApiPath);
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
                deletedTranslation = JsonConvert.DeserializeObject<TextTranslation>(deleteResponseString);
                List<KinaUnaLanguage> languages = await GetAllLanguages();
                foreach (KinaUnaLanguage language in languages)
                {
                    if (deletedTranslation != null) await _cache.RemoveAsync("PageTranslations" + deletedTranslation.Page + "&Lang" + language.Id);
                    _ = await GetAllTranslations(language.Id, true).ConfigureAwait(false);
                }
            }

            return deletedTranslation;
        }

        public async Task<List<TextTranslation>> GetAllTranslations(int languageId = 0, bool updateCache = false)
        {
            List<TextTranslation> translationsList = new();
            string cachedTranslationsList = await _cache.GetStringAsync("AllTranslations" + "&Lang" + languageId);
            if (!updateCache && languageId != 0 && !string.IsNullOrEmpty(cachedTranslationsList))
            {
                translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(cachedTranslationsList);
            }
            else
            {
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string admininfoApiPath = "/api/Translations/GetAllTranslations/" + languageId;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (admininfoResponse.IsSuccessStatusCode)
                {
                    string translationsListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                    translationsList = JsonConvert.DeserializeObject<List<TextTranslation>>(translationsListAsString);
                    await _cache.SetStringAsync("AllTranslations" + "&Lang" + languageId, JsonConvert.SerializeObject(translationsList), _cacheExpirationLong);
                }
            }

            return translationsList;
        }

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
                string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(true);
                _httpClient.SetBearerToken(accessToken);

                string admininfoApiPath = "/api/Translations/GetTranslationById/" + id;
                HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

                if (admininfoResponse.IsSuccessStatusCode)
                {
                    string translationAsString = await admininfoResponse.Content.ReadAsStringAsync();
                    textTranslation = JsonConvert.DeserializeObject<TextTranslation>(translationAsString);
                    await _cache.SetStringAsync("TranslationById" + id, JsonConvert.SerializeObject(textTranslation), _cacheExpirationLong);
                }
            }

            return textTranslation;
        }
    }
}
