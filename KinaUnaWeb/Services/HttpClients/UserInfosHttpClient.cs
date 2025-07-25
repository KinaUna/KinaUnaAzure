using Duende.IdentityModel.Client;
using KinaUna.Data;
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
    /// Provides methods to interact with the UserInfos API.
    /// </summary>
    public class UserInfosHttpClient : IUserInfosHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;

        public UserInfosHttpClient(HttpClient httpClient, IConfiguration configuration, IHostEnvironment env, IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;

            string clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey);
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Local");
            }

            if (env.IsStaging())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Azure");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets a user's UserInfo from the email address.
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <returns>The UserInfo with the given email address. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        public async Task<UserInfo> GetUserInfo(string email)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string userInfoApiPath = "api/UserInfo/UserInfoByEmail/";
            
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo;
        }

        /// <summary>
        /// Gets a user's information from the UserId.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>The UserInfo with the given UserId. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        public async Task<UserInfo> GetUserInfoByUserId(string userId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string userInfoApiPath = "api/UserInfo/ByUserIdPost/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();
            
            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
            return userInfo ?? new UserInfo();
        }

        /// <summary>
        /// Adds a new UserInfo object.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to add.</param>
        /// <returns>The added UserInfo object. If an error occurs a new UserInfo with Id=0 is returned.</returns>
        public async Task<UserInfo> AddUserInfo(UserInfo userInfo)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string newUserInfoApiPath = "/api/UserInfo/";
            HttpResponseMessage newUserInfoResponse = await _httpClient.PostAsync(newUserInfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!newUserInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string newUserResponseString = await newUserInfoResponse.Content.ReadAsStringAsync();
            UserInfo addedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return addedUserinfo ?? new UserInfo();
        }

        /// <summary>
        /// Updates a UserInfo object. The UserInfo with the same Id will be updated.
        /// </summary>
        /// <param name="userInfo">The UserInfo object with the updated properties.</param>
        /// <returns>UserInfo: The updated UserInfo object. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        public async Task<UserInfo> UpdateUserInfo(UserInfo userInfo)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string newUserInfoApiPath = "/api/UserInfo/0";
            HttpResponseMessage newUserInfoResponse = await _httpClient.PutAsync(newUserInfoApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!newUserInfoResponse.IsSuccessStatusCode) return new UserInfo();
            
            string newUserResponseString = await newUserInfoResponse.Content.ReadAsStringAsync();
            UserInfo updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo ?? new UserInfo();
        }

        /// <summary>
        /// Deletes a UserInfo object.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to delete.</param>
        /// <returns>The deleted UserInfo object. If not found or an error occurs a new UserInfo with Id=0 is returned.</returns>
        public async Task<UserInfo> DeleteUserInfo(UserInfo userInfo)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            userInfo.Deleted = true;

            string deleteApiPath = "/api/UserInfo/" + userInfo.UserId;

            HttpResponseMessage deleteResponse = await _httpClient.PutAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return new UserInfo();

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            UserInfo deletedUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseString);
            return deletedUserInfo ?? new UserInfo();
        }

        /// <summary>
        /// Checks if the current user's account is active.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>If the user is still active the UserInfo object of the user. If inactive a new UserInfo object with Id=0.</returns>
        public async Task<UserInfo> CheckCurrentUser(string userId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string userinfoApiPath = "/api/UserInfo/CheckCurrentUser/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userinfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userInfoResponseAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseAsString);
            return userInfo;
        }

        /// <summary>
        /// Gets the list of all soft-deleted UserInfos.
        /// Only KinaUnaAdmins are allowed to get all deleted UserInfo entities.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        public async Task<List<UserInfo>> GetDeletedUserInfos()
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string userInfoApiPath = "/api/UserInfo/GetDeletedUserInfos/";
            HttpResponseMessage userInfoResponse = await _httpClient.GetAsync(userInfoApiPath);
            List<UserInfo> userInfosList = [];
            if (!userInfoResponse.IsSuccessStatusCode) return userInfosList;

            string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            userInfosList = JsonConvert.DeserializeObject<List<UserInfo>>(userInfoAsString);

            return userInfosList;
        }

        /// <summary>
        /// Permanently deletes a UserInfo entity.
        /// To soft-delete a UserInfo use update method and set the Deleted property to true.
        /// </summary>
        /// <param name="userInfo">The UserInfo entity to delete.</param>
        /// <returns>The deleted UserInfo object.</returns>
        public async Task<UserInfo> RemoveUserInfoForGood(UserInfo userInfo)
        {
            UserInfo deletedUserInfo = new();
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string deleteApiPath = "/api/UserInfo/" + userInfo.UserId;

            HttpResponseMessage deleteResponse = await _httpClient.DeleteAsync(deleteApiPath);
            if (!deleteResponse.IsSuccessStatusCode) return deletedUserInfo;

            string deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedUserInfo = JsonConvert.DeserializeObject<UserInfo>(deleteResponseString);

            return deletedUserInfo;
        }

        /// <summary>
        /// Get a list of all UserInfos.
        /// Includes soft-deleted entities.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        public async Task GetAllUserInfos()
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userInfosApiPath = "/api/UserInfo/GetAll";
            await _httpClient.GetAsync(userInfosApiPath);
        }
    }
}
