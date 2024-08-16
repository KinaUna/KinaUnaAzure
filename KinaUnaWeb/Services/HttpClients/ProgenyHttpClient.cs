using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Progeny API.
    /// Contains the methods for adding, retrieving and updating progeny and user data.
    /// </summary>
    public class ProgenyHttpClient : IProgenyHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public ProgenyHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }

        /// <summary>
        /// Gets the Progeny with the given Id.
        /// </summary>
        /// <param name="progenyId">The Progeny's Id.</param>
        /// <returns>Progeny object with the given Id. If not found, a new Progeny object with Id=0 is returned.</returns>
        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Progeny progeny = new();
            string progenyApiPath = "/api/Progeny/" + progenyId;

            try
            {
                HttpResponseMessage progenyResponse = await _httpClient.GetAsync(progenyApiPath);

                if (progenyResponse.IsSuccessStatusCode)
                {
                    string progenyAsString = await progenyResponse.Content.ReadAsStringAsync();
                    progeny = JsonConvert.DeserializeObject<Progeny>(progenyAsString);
                }
                else
                {
                    progeny.Name = "401";

                }
            }
            catch (Exception e)
            {
                if (progeny != null)
                {
                    progeny.Name = "401";
                    progeny.NickName = e.Message;
                    return progeny;
                }
            }

            return progeny;
        }

        /// <summary>
        /// Adds a new Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object to be added.</param>
        /// <returns>Progeny: The Progeny object that was added.</returns>
        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string newProgenyApiPath = "/api/Progeny/";
            HttpResponseMessage progenyResponse = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string newProgeny = await progenyResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Progeny>(newProgeny);

        }

        /// <summary>
        /// Updates a Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object with the updated properties.</param>
        /// <returns>The updated Progeny object.</returns>
        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateProgenyApiPath = "/api/Progeny/" + progeny.Id;
            HttpResponseMessage progenyResponse = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string updateProgenyResponseString = await progenyResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);

        }

        /// <summary>
        /// Removes a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to be removed.</param>
        /// <returns>bool: True if successfully removed.</returns>
        public async Task<bool> DeleteProgeny(int progenyId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string progenyApiPath = "/api/Progeny/" + progenyId;
            HttpResponseMessage progenyResponse = await _httpClient.DeleteAsync(progenyApiPath);
            return progenyResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string accessApiPath = "/api/Access/AdminListByUserPost/";
            List<Progeny> accessList = [];
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessResponseString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }

        /// <summary>
        /// Gets the latest 5 posts (timeline time, not added time) for a Progeny, that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public async Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyPosts = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string latestApiPath = "/api/Timeline/ProgenyLatest/" + progenyId + "/" + accessLevel + "/5/0";
            HttpResponseMessage latestResponse = await _httpClient.GetAsync(latestApiPath);
            if (!latestResponse.IsSuccessStatusCode) return progenyPosts;

            string latestAsString = await latestResponse.Content.ReadAsStringAsync();

            progenyPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(latestAsString);

            return progenyPosts;
        }

        /// <summary>
        /// Gets all the posts with today's day of month and month, for all years (timeline time, not added time), that the user has access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public async Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel)
        {
            List<TimeLineItem> yearAgoPosts = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string yearAgoApiPath = "/api/Timeline/ProgenyYearAgo/" + progenyId + "/" + accessLevel;
            HttpResponseMessage yearAgoResponse = await _httpClient.GetAsync(yearAgoApiPath);
            if (!yearAgoResponse.IsSuccessStatusCode) return yearAgoPosts;

            string yearAgoAsString = await yearAgoResponse.Content.ReadAsStringAsync();

            yearAgoPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(yearAgoAsString);

            return yearAgoPosts;
        }
    }
}
