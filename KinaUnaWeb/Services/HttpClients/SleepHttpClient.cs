using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class SleepHttpClient : ISleepHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public SleepHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<Sleep> GetSleepItem(int sleepId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Sleep sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleepItem ?? new Sleep();
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string sleepApiPath = "/api/Sleep/";
            HttpResponseMessage sleepResponse = await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep ?? new Sleep();
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateSleepApiPath = "/api/Sleep/" + sleep.SleepId;
            HttpResponseMessage sleepResponse = await _httpClient.PutAsync(updateSleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep ?? new Sleep();
        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage deleteSleepResponse = await _httpClient.DeleteAsync(sleepApiPath);
            return deleteSleepResponse.IsSuccessStatusCode;
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> progenySleepList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
