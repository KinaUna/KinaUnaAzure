﻿using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public class AutoSuggestsHttpClient : IAutoSuggestsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public AutoSuggestsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<List<string>> GetTagsList(int progenyId, int accessLevel)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultTagsList = [];

            string tagsApiPath = "/api/AutoSuggests/GetTagsAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage tagsResponse = await _httpClient.GetAsync(tagsApiPath).ConfigureAwait(false);
            if (!tagsResponse.IsSuccessStatusCode) return resultTagsList;

            string tagsListAsString = await tagsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultTagsList = JsonConvert.DeserializeObject<List<string>>(tagsListAsString);

            return resultTagsList;
        }

        public async Task<List<string>> GetContextsList(int progenyId, int accessLevel)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultContextsList = [];

            string contextsApiPath = "/api/AutoSuggests/GetContextAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage contextsResponse = await _httpClient.GetAsync(contextsApiPath).ConfigureAwait(false);
            if (!contextsResponse.IsSuccessStatusCode) return resultContextsList;

            string contextsListAsString = await contextsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultContextsList = JsonConvert.DeserializeObject<List<string>>(contextsListAsString);

            return resultContextsList;
        }

        public async Task<List<string>> GetLocationsList(int progenyId, int accessLevel)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultLocationsList = [];

            string locationsApiPath = "/api/AutoSuggests/GetLocationAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (!locationsResponse.IsSuccessStatusCode) return resultLocationsList;

            string locationsListAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultLocationsList = JsonConvert.DeserializeObject<List<string>>(locationsListAsString);

            return resultLocationsList;
        }

        public async Task<List<string>> GetCategoriesList(int progenyId, int accessLevel)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultCategoriesList = [];

            string categoriesApiPath = "/api/AutoSuggests/GetCategoryAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage categoriesResponse = await _httpClient.GetAsync(categoriesApiPath).ConfigureAwait(false);
            if (!categoriesResponse.IsSuccessStatusCode) return resultCategoriesList;

            string categoriesListAsString = await categoriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            resultCategoriesList = JsonConvert.DeserializeObject<List<string>>(categoriesListAsString);

            return resultCategoriesList;
        }
    }
}
