using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class SleepHttpClient: ISleepHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public SleepHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration.");

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

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration."));
            return accessToken;
        }

        public async Task<Sleep?> GetSleepItem(int sleepId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Sleep? sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleepItem;

        }

        public async Task<Sleep?> AddSleep(Sleep? sleep)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string sleepApiPath = "/api/Sleep/";
            HttpResponseMessage sleepResponse = await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep;

        }

        public async Task<Sleep?> UpdateSleep(Sleep? sleep)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateSleepApiPath = "/api/Sleep/" + sleep?.SleepId;
            HttpResponseMessage sleepResponse = await _httpClient.PutAsync(updateSleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep;

        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage deleteSleepResponse = await _httpClient.DeleteAsync(sleepApiPath);
            return deleteSleepResponse.IsSuccessStatusCode;
        }

        public async Task<List<Sleep>?> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep>? progenySleepList = [];
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath);
            if (!sleepResponse.IsSuccessStatusCode) return progenySleepList;

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            progenySleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepAsString);

            return progenySleepList;
        }
    }
}
