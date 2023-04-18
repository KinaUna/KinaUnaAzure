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

namespace KinaUnaWeb.Services
{
    public class LanguagesHttpClient : ILanguagesHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheExpirationLong = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(22));

        public LanguagesHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IDistributedCache cache)
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

        public async Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false)
        {
            List<KinaUnaLanguage> languageList = new();
            string cachedLanguagesString = await _cache.GetStringAsync("AllLanguages");
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguagesString))
            {
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(cachedLanguagesString);
                return languageList;
            }

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string admininfoApiPath = "/api/Languages/GetAllLanguages";
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (admininfoResponse.IsSuccessStatusCode)
            {
                string languageListAsString = await admininfoResponse.Content.ReadAsStringAsync();
                languageList = JsonConvert.DeserializeObject<List<KinaUnaLanguage>>(languageListAsString);
                if (languageList != null && languageList.Any())
                {
                    await _cache.SetStringAsync("AllLanguages", JsonConvert.SerializeObject(languageList));
                }
            }

            return languageList;
        }

        public async Task<KinaUnaLanguage> GetLanguage(int languageId, bool updateCache = false)
        {
            KinaUnaLanguage language = new();
            string cachedLanguageString = await _cache.GetStringAsync("Language" + languageId);
            if (!updateCache && !string.IsNullOrEmpty(cachedLanguageString))
            {
                language = JsonConvert.DeserializeObject<KinaUnaLanguage>(cachedLanguageString);
                return language;
            }

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string admininfoApiPath = "/api/Languages/GetLanguage/" + languageId;
            HttpResponseMessage admininfoResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (admininfoResponse.IsSuccessStatusCode)
            {
                string languageAsString = await admininfoResponse.Content.ReadAsStringAsync();
                language = JsonConvert.DeserializeObject<KinaUnaLanguage>(languageAsString);
                await _cache.SetStringAsync("Language" + languageId, JsonConvert.SerializeObject(language), _cacheExpirationLong);
            }

            return language;
        }

        public async Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage addedLanguage = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Languages/AddLanguage/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(language), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                string addResponseString = await addResponse.Content.ReadAsStringAsync();
                addedLanguage = JsonConvert.DeserializeObject<KinaUnaLanguage>(addResponseString);
            }

            return addedLanguage;
        }


        public async Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage updatedLanguage = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateApiPath = "/api/Languages/UpdateLanguage/" + language.Id;
            HttpResponseMessage updateResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(language), System.Text.Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode)
            {
                string updateResponseString = await updateResponse.Content.ReadAsStringAsync();
                updatedLanguage = JsonConvert.DeserializeObject<KinaUnaLanguage>(updateResponseString);
            }

            return updatedLanguage;
        }

        public async Task<KinaUnaLanguage> DeleteLanguage(KinaUnaLanguage language)
        {
            KinaUnaLanguage deletedLanguage = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string deleteApiPath = "/api/Languages/DeleteLanguage/" + language.Id;
            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(deleteApiPath);
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deletedResponseString = await deleteResponse.Content.ReadAsStringAsync();
                deletedLanguage = JsonConvert.DeserializeObject<KinaUnaLanguage>(deletedResponseString);
                await _cache.RemoveAsync("Language" + language.Id);
                await _cache.RemoveAsync("AllLanguages");
            }

            return deletedLanguage;
        }
    }
}
