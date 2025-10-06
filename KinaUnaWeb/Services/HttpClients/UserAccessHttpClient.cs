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
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the UserAccess API.
    /// </summary>
    public class UserAccessHttpClient : IUserAccessHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;

        public UserAccessHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
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
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">The UserAccess object to be added.</param>
        /// <returns>The UserAccess object that was added. If an error occurs, a new UserAccess with AccessId = 0.</returns>
        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<UserAccess> accessList = [];
            string accessApiPath = "/api/Access/AccessListByUser/";
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userEmail), System.Text.Encoding.UTF8, "application/json"));
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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string accessApiPath = "/api/Access/" + accessId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (!accessResponse.IsSuccessStatusCode) return new UserAccess();

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            UserAccess accessItem = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
            return accessItem ?? new UserAccess();
        }

        /// <summary>
        /// Retrieves a list of permissions for a specific timeline item.
        /// </summary>
        /// <remarks>This method retrieves the permissions for a timeline item by making an HTTP request
        /// to the Access Management API.  The caller must ensure that the user is authenticated, as the method requires
        /// a valid access token to perform the operation.</remarks>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/> enumeration.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="TimelineItemPermission"/> objects representing the permissions for the specified timeline item. If the
        /// operation fails, an empty list is returned.</returns>
        public async Task<List<TimelineItemPermission>> GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType itemType, int itemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string accessManagementApiPath = "/api/AccessManagement/GetTimelineItemPermissionsList/" + (int)itemType + "/" + itemId;
            HttpResponseMessage accessManagementResponse = await _httpClient.GetAsync(accessManagementApiPath);
            if (!accessManagementResponse.IsSuccessStatusCode) return new List<TimelineItemPermission>();

            string permissionListAsString = await accessManagementResponse.Content.ReadAsStringAsync();
            List<TimelineItemPermission> timelineItemPermissions = JsonConvert.DeserializeObject<List<TimelineItemPermission>>(permissionListAsString);
            return timelineItemPermissions;
        }
    }
}
