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
    /// <summary>
    /// Provides methods to interact with the UserAccess API.
    /// </summary>
    public class UserAccessHttpClient : IUserAccessHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public UserAccessHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
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
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">The UserAccess object to be added.</param>
        /// <returns>The UserAccess object that was added. If an error occurs, a new UserAccess with AccessId = 0.</returns>
        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string accessApiPath = "/api/Access/";
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return new UserAccess();

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            userAccess = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
            return userAccess ?? new UserAccess();
        }

        /// <summary>
        /// Updates a UserAccess object. The UserAccess with the same AccessId will be updated.
        /// </summary>
        /// <param name="userAccess">The UserAccess object with the updated properties.</param>
        /// <returns>The updated UserAccess object. If not found or an error occurs, a new UserAccess with AccessId = 0.</returns>
        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateAccessApiPath = "/api/Access/" + userAccess.AccessId;
            HttpResponseMessage accessResponse = await _httpClient.PutAsync(updateAccessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return new UserAccess();

            string userAccessAsString = await accessResponse.Content.ReadAsStringAsync();
            userAccess = JsonConvert.DeserializeObject<UserAccess>(userAccessAsString);
            return userAccess ?? new UserAccess();
        }

        /// <summary>
        /// Deletes a UserAccess object.
        /// </summary>
        /// <param name="userAccessId">The UserAccess object's AccessId.</param>
        /// <returns>bool: True if the UserAccess object was successfully deleted.</returns>
        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + userAccessId;
            HttpResponseMessage accessTokenResponse = await _httpClient.DeleteAsync(accessApiPath);
            return accessTokenResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of UserAccess for a progeny.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <returns>List of UserAccess objects.</returns>
        public async Task<List<UserAccess>> GetProgenyAccessList(int progenyId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess> accessList = [];
            string accessApiPath = "/api/Access/Progeny/" + progenyId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);

            return accessList;
        }

        /// <summary>
        /// Gets the list of UserAccess for a user.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        public async Task<List<UserAccess>> GetUserAccessList(string userEmail)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess> accessList = [];
            string accessApiPath = "/api/Access/AccessListByUser/" + userEmail;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);

            return accessList;
        }

        /// <summary>
        /// Gets the UserAccess with a given AccessId.
        /// </summary>
        /// <param name="accessId">The AccessId of the UserAccess.</param>
        /// <returns>The UserAccess with the given AccessId. If not found or an error occurs, a new UserAccess with AccessId = 0.</returns>
        public async Task<UserAccess> GetUserAccess(int accessId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + accessId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (!accessResponse.IsSuccessStatusCode) return new UserAccess();

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            UserAccess accessItem = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
            return accessItem ?? new UserAccess();
        }
    }
}
