using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class AuthHttpClient:IAuthHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            IConfiguration configuration1 = configuration;
            string clientUri = configuration1.GetValue<string>("AuthenticationServer") ?? throw new InvalidOperationException("AuthenticationServer value missing in configuration");
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }
        
        
        public async Task<UserInfo?> CheckDeleteUser(UserInfo userInfo)
        {
            string deleteAccountPath = "/Account/CheckDeleteKinaUnaAccount/";
            
            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deleteResponseAsString = await deleteResponse.Content.ReadAsStringAsync();
                UserInfo? resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseAsString);
                if (resultUserInfo?.UserId == userInfo.UserId)
                {
                    return resultUserInfo;
                }
            }

            return new UserInfo();
        }

        public async Task<UserInfo?> RemoveDeleteUser(UserInfo userInfo)
        {
            string accessToken = "";
            HttpContext? currentContext = _httpContextAccessor.HttpContext;

            if (currentContext != null)
            {
                string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                if (!string.IsNullOrWhiteSpace(contextAccessToken))
                {
                    accessToken = contextAccessToken;
                }
            }

            _httpClient.SetBearerToken(accessToken);
            string deleteAccountPath = "/Account/RemoveDeleteKinaUnaAccount/";

            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deleteResponseAsString = await deleteResponse.Content.ReadAsStringAsync();
                UserInfo? resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseAsString);
                if (resultUserInfo?.UserId == userInfo.UserId)
                {
                    return resultUserInfo;
                }
            }

            return new UserInfo();
        }

        public async Task<bool> IsApplicationUserValid(string userId)
        {
            string checkAccountPath = "/Account/IsApplicationUserValid/";
            UserInfo userInfo = new UserInfo();
            userInfo.UserId = userId;
            HttpResponseMessage checkResponse = await _httpClient.PostAsync(checkAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (checkResponse.IsSuccessStatusCode)
            {
                string checkResponseAsString = await checkResponse.Content.ReadAsStringAsync();
                UserInfo? resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(checkResponseAsString);
                if (resultUserInfo?.UserId == userInfo.UserId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
