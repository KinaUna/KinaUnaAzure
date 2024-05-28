using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class UserInfosHttpClient: IUserInfosHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public UserInfosHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
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

        public async Task<UserInfo?> GetUserInfo(string email)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string userInfoApiPath = "api/UserInfo/UserInfoByEmail/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));

            UserInfo? userInfo = new();
            if (!userInfoResponse.IsSuccessStatusCode) return userInfo;

            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo;

        }

        public async Task<UserInfo?> GetUserInfoByUserId(string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string userInfoApiPath = "api/UserInfo/ByUserIdPost/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo? userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo;

        }
        public async Task<UserInfo?> UpdateUserInfo(UserInfo userInfo)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            const string newUserInfoApiPath = "/api/UserInfo/";
            HttpResponseMessage newUserInfoResponse = await _httpClient.PutAsync(newUserInfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));

            if (!newUserInfoResponse.IsSuccessStatusCode) return new UserInfo();
            string newUserResponseString = await newUserInfoResponse.Content.ReadAsStringAsync();
            UserInfo? updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;

        }

        public async Task<UserInfo?> DeleteUserInfo(UserInfo userInfo)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            userInfo.Deleted = true;

            string deleteApiPath = "/api/UserInfo/" + userInfo.UserId;

            HttpResponseMessage deleteResponse = await _httpClient.PutAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return new UserInfo();

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            UserInfo? deletedUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseString);
            return deletedUserInfo;

        }

        public async Task<List<UserInfo>?> GetDeletedUserInfos()
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string userInfoApiPath = "/api/UserInfo/GetDeletedUserInfos/";
            HttpResponseMessage userInfoResponse = await _httpClient.GetAsync(userInfoApiPath);
            List<UserInfo>? userInfosList = [];
            if (!userInfoResponse.IsSuccessStatusCode) return userInfosList;

            string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            userInfosList = JsonConvert.DeserializeObject<List<UserInfo>>(userInfoAsString);

            return userInfosList;
        }

        public async Task<UserInfo?> RemoveUserInfoForGood(UserInfo userInfo)
        {
            UserInfo? deletedUserInfo = new();
            string accessToken = await GetNewToken();
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
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string setChildApiPath = "/api/UserInfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userInfo);
        }
    }
}
