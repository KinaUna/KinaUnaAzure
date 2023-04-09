using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaWeb.Services
{
    public class AutoSuggestsHttpClient:IAutoSuggestsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public AutoSuggestsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
           
        }

        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");

            string accessToken = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<List<string>> GetTagsList(int progenyId, int accessLevel)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultTagsList = new();

            string tagsApiPath = "/api/AutoSuggests/GetTagsAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage tagsResponse = await _httpClient.GetAsync(tagsApiPath).ConfigureAwait(false);
            if (tagsResponse.IsSuccessStatusCode)
            {
                string tagsListAsString = await tagsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                resultTagsList = JsonConvert.DeserializeObject<List<string>>(tagsListAsString);
            }

            return resultTagsList;
        }

        public async Task<List<string>> GetContextsList(int progenyId, int accessLevel)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultContextsList = new();

            string contextsApiPath = "/api/AutoSuggests/GetContextAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage contextsResponse = await _httpClient.GetAsync(contextsApiPath).ConfigureAwait(false);
            if (contextsResponse.IsSuccessStatusCode)
            {
                string contextsListAsString = await contextsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                resultContextsList = JsonConvert.DeserializeObject<List<string>>(contextsListAsString);
            }

            return resultContextsList;
        }

        public async Task<List<string>> GetLocationsList(int progenyId, int accessLevel)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultLocationsList = new();

            string locationsApiPath = "/api/AutoSuggests/GetLocationAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (locationsResponse.IsSuccessStatusCode)
            {
                string locationsListAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                resultLocationsList = JsonConvert.DeserializeObject<List<string>>(locationsListAsString);
            }

            return resultLocationsList;
        }

        public async Task<List<string>> GetCategoriesList(int progenyId, int accessLevel)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<string> resultCategoriesList = new();

            string categoriesApiPath = "/api/AutoSuggests/GetCategoryAutoSuggestList/" + progenyId + "/" + accessLevel;

            HttpResponseMessage categoriesResponse = await _httpClient.GetAsync(categoriesApiPath).ConfigureAwait(false);
            if (categoriesResponse.IsSuccessStatusCode)
            {
                string categoriesListAsString = await categoriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                resultCategoriesList = JsonConvert.DeserializeObject<List<string>>(categoriesListAsString);
            }

            return resultCategoriesList;
        }
    }
}
