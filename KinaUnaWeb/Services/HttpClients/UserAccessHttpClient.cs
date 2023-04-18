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

        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/";
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                userAccess = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
                if (userAccess != null)
                {
                    return userAccess;
                }
            }

            return new UserAccess();
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateAccessApiPath = "/api/Access/" + userAccess.AccessId;
            HttpResponseMessage accessResponse = await _httpClient.PutAsync(updateAccessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (accessResponse.IsSuccessStatusCode)
            {
                string userAccessAsString = await accessResponse.Content.ReadAsStringAsync();
                userAccess = JsonConvert.DeserializeObject<UserAccess>(userAccessAsString);
                if (userAccess != null)
                {
                    return userAccess;
                }
            }

            return new UserAccess();
        }

        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + userAccessId;
            HttpResponseMessage accessTokenResponse = await _httpClient.DeleteAsync(accessApiPath);
            if (accessTokenResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<UserAccess>> GetProgenyAccessList(int progenyId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess> accessList = new();
            string accessApiPath = "/api/Access/Progeny/" + progenyId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> GetUserAccessList(string userEmail)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess> accessList = new();
            string accessApiPath = "/api/Access/AccessListByUser/" + userEmail;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int userAccessId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + userAccessId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                UserAccess accessItem = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
                if (accessItem != null)
                {
                    return accessItem;
                }
            }

            return new UserAccess();
        }
    }
}
