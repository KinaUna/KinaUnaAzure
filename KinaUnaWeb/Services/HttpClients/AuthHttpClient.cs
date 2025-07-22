using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the IDP/Authentication Server.
    /// </summary>
    public class AuthHttpClient : IAuthHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            // Todo: Update to use separate configuration for Development and Production environments.
            // Todo: Update to use OpenIdDict.
            string clientUri = configuration.GetValue<string>("AuthenticationServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("AuthenticationServerLocal");
            }
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Checks if a UserInfo has been soft-deleted.
        /// Soft-deleted users are stored in the DeletedUsers table in the database.
        /// Soft-deleted users should be permanently deleted from the IDP accounts database after a certain time period.
        /// </summary>
        /// <param name="userInfo">The UserInfo for the user.</param>
        /// <returns>If a deleted UserInfo is found it is returned, else a new UserInfo with an empty string for UserId is returned.</returns>
        public async Task<UserInfo> CheckDeleteUser(UserInfo userInfo)
        {
            const string deleteAccountPath = "/Account/CheckDeleteKinaUnaAccount/";

            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return new UserInfo();

            string deleteResponseAsString = await deleteResponse.Content.ReadAsStringAsync();
            UserInfo resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseAsString);
            if (resultUserInfo != null && resultUserInfo.UserId == userInfo.UserId)
            {
                return resultUserInfo;
            }

            return new UserInfo();
        }

        /// <summary>
        /// Removes a soft-deleted UserInfo from the DeletedUsers table in the database.
        /// This should be called when a user has soft-deleted their account and wants to restore it.
        /// </summary>
        /// <param name="userInfo">The UserInfo of the user.</param>
        /// <returns>The UserInfo that was soft-deleted and needs to be restored. If it doesn't exist in the DeletedUsers table a new UserInfo with an empty string for UserId is returned.</returns>
        public async Task<UserInfo> RemoveDeleteUser(UserInfo userInfo)
        {
            string accessToken = "";
            HttpContext currentContext = _httpContextAccessor.HttpContext;

            if (currentContext != null)
            {
                string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                if (!string.IsNullOrWhiteSpace(contextAccessToken))
                {
                    accessToken = contextAccessToken;
                }
            }

            _httpClient.SetBearerToken(accessToken);
            const string deleteAccountPath = "/Account/RemoveDeleteKinaUnaAccount/";

            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return new UserInfo();

            string deleteResponseAsString = await deleteResponse.Content.ReadAsStringAsync();
            UserInfo resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseAsString);
            if (resultUserInfo != null && resultUserInfo.UserId == userInfo.UserId)
            {
                return resultUserInfo;
            }

            return new UserInfo();
        }

        /// <summary>
        /// Checks if a user is a valid ApplicationUser in the IDP database.
        /// This is to prevent access if a user's UserInfo hasn't been deleted but the ApplicationUser has been deleted.
        /// </summary>
        /// <param name="userId">The UserId of the user.</param>
        /// <returns>True if the user exists, false if the user doesn't exist.</returns>
        public async Task<bool> IsApplicationUserValid(string userId)
        {
            const string checkAccountPath = "/Account/IsApplicationUserValid/";
            UserInfo userInfo = new()
            {
                UserId = userId
            };

            HttpResponseMessage checkResponse = await _httpClient.PostAsync(checkAccountPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!checkResponse.IsSuccessStatusCode) return false;

            string checkResponseAsString = await checkResponse.Content.ReadAsStringAsync();
            UserInfo resultUserInfo = JsonConvert.DeserializeObject<UserInfo>(checkResponseAsString);
            return resultUserInfo != null && resultUserInfo.UserId == userInfo.UserId;
        }
    }
}
