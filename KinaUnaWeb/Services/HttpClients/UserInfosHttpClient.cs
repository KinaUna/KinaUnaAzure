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
    public class UserInfosHttpClient : IUserInfosHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public UserInfosHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<UserInfo> GetUserInfo(string email)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string userInfoApiPath = "api/UserInfo/UserInfoByEmail/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string userId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string userInfoApiPath = "api/UserInfo/ByUserIdPost/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();
            
            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo ?? new UserInfo();
        }

        public async Task<UserInfo> AddUserInfo(UserInfo userInfo)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string newUserInfoApiPath = "/api/UserInfo/";
            HttpResponseMessage newUserInfoResponse = await _httpClient.PostAsync(newUserInfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!newUserInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string newUserResponseString = await newUserInfoResponse.Content.ReadAsStringAsync();
            UserInfo addedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return addedUserinfo ?? new UserInfo();
        }

        public async Task<UserInfo> UpdateUserInfo(UserInfo userInfo)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string newUserInfoApiPath = "/api/UserInfo/";
            HttpResponseMessage newUserInfoResponse = await _httpClient.PutAsync(newUserInfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!newUserInfoResponse.IsSuccessStatusCode) return new UserInfo();
            
            string newUserResponseString = await newUserInfoResponse.Content.ReadAsStringAsync();
            UserInfo updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo ?? new UserInfo();
        }

        public async Task<UserInfo> DeleteUserInfo(UserInfo userInfo)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            userInfo.Deleted = true;

            string deleteApiPath = "/api/UserInfo/" + userInfo.UserId;

            HttpResponseMessage deleteResponse = await _httpClient.PutAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return new UserInfo();

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            UserInfo deletedUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseString);
            return deletedUserInfo ?? new UserInfo();
        }

        public async Task<UserInfo> CheckCurrentUser(string userId)
        {
            const string userinfoApiPath = "/api/UserInfo/CheckCurrentUser/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userinfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userInfoResponseAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseAsString);
            return userInfo;

        }

        public async Task<List<UserInfo>> GetDeletedUserInfos()
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string userInfoApiPath = "/api/UserInfo/GetDeletedUserInfos/";
            HttpResponseMessage userInfoResponse = await _httpClient.GetAsync(userInfoApiPath);
            List<UserInfo> userInfosList = [];
            if (!userInfoResponse.IsSuccessStatusCode) return userInfosList;

            string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            userInfosList = JsonConvert.DeserializeObject<List<UserInfo>>(userInfoAsString);

            return userInfosList;
        }

        public async Task<UserInfo> RemoveUserInfoForGood(UserInfo userInfo)
        {
            UserInfo deletedUserInfo = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string deleteApiPath = "/api/UserInfo/" + userInfo.UserId;

            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(deleteApiPath);
            if (!deleteResponse.IsSuccessStatusCode) return deletedUserInfo;

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseString);

            return deletedUserInfo;
        }

        public async Task SetViewChild(string userId, UserInfo userInfo)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string setChildApiPath = "/api/UserInfo/GetAll" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userInfo);
        }

        public async Task GetAllUserInfos()
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string userInfosApiPath = "/api/UserInfo/GetAll";
            await _httpClient.GetAsync(userInfosApiPath);
        }
    }
}
