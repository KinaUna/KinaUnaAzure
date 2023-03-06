using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
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

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName, _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<Sleep> GetSleepItem(int sleepId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                string sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                Sleep sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
                if (sleepItem != null)
                {
                    return sleepItem;
                }
            }

            return new Sleep();
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/";
            HttpResponseMessage sleepResponse = await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (sleepResponse.IsSuccessStatusCode)
            {
                string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
                sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
                if (sleep != null)
                {
                    return sleep;
                }
            }

            return new Sleep();
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateSleepApiPath = "/api/Sleep/" + sleep.SleepId;
            HttpResponseMessage sleepResponse = await _httpClient.PutAsync(updateSleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (sleepResponse.IsSuccessStatusCode)
            {
                string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
                sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
                if (sleep != null)
                {
                    return sleep;
                }
            }

            return new Sleep();
        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage deleteSleepResponse = await _httpClient.DeleteAsync(sleepApiPath);
            if (deleteSleepResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> progenySleepList = new List<Sleep>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath);
            if (sleepResponse.IsSuccessStatusCode)
            {
                string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
                progenySleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepAsString);
            }

            return progenySleepList;
        }
    }
}
