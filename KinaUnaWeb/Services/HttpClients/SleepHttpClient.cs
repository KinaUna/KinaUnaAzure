using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Sleep API.
    /// </summary>
    public class SleepHttpClient : ISleepHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SleepHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets a Sleep with a given SleepId.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep to get.</param>
        /// <returns>The Sleep object with the given SleepId. If the item cannot be found a new Sleep object with SleepId=0 is returned.</returns>
        public async Task<Sleep> GetSleepItem(int sleepId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Sleep sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleepItem ?? new Sleep();
        }

        /// <summary>
        /// Adds a new Sleep object.
        /// </summary>
        /// <param name="sleep">The Sleep object to be added.</param>
        /// <returns>The added Sleep object.</returns>
        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string sleepApiPath = "/api/Sleep/";
            HttpResponseMessage sleepResponse = await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep ?? new Sleep();
        }

        /// <summary>
        /// Updates a Sleep object. The Sleep with the same SleepId will be updated.
        /// </summary>
        /// <param name="sleep">The Sleep object with the updated properties.</param>
        /// <returns>The updated Sleep object.</returns>
        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateSleepApiPath = "/api/Sleep/" + sleep.SleepId;
            HttpResponseMessage sleepResponse = await _httpClient.PutAsync(updateSleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json"));
            if (!sleepResponse.IsSuccessStatusCode) return new Sleep();

            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            sleep = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            return sleep ?? new Sleep();
        }

        /// <summary>
        /// Deletes the Sleep object with a given SleepId.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep object to delete.</param>
        /// <returns>bool: True if the Sleep object was successfully deleted.</returns>
        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string sleepApiPath = "/api/Sleep/" + sleepId;
            HttpResponseMessage deleteSleepResponse = await _httpClient.DeleteAsync(sleepApiPath);
            return deleteSleepResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the List of all Sleep objects for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <returns>List of Sleep objects.</returns>
        public async Task<List<Sleep>> GetSleepList(int progenyId)
        {
            List<Sleep> progenySleepList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string sleepApiPath = "/api/Sleep/Progeny/" + progenyId;
            HttpResponseMessage sleepResponse = await _httpClient.GetAsync(sleepApiPath);
            if (!sleepResponse.IsSuccessStatusCode) return progenySleepList;
            string sleepAsString = await sleepResponse.Content.ReadAsStringAsync();
            progenySleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepAsString);

            return progenySleepList;
        }
    }
}
