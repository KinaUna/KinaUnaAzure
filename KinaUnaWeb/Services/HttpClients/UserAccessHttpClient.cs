using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.AccessManagement;
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

        public async Task<bool> ConvertUserAccessesToUserGroups()
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            _httpClient.Timeout = TimeSpan.FromMinutes(20);
            
            string accessApiPath = "/api/Access/ConvertUserAccessesToUserGroups/";
            HttpResponseMessage accessManagementResponse = await _httpClient.GetAsync(accessApiPath);

            _httpClient.Timeout = TimeSpan.FromSeconds(100);
            
            if (!accessManagementResponse.IsSuccessStatusCode) return false;
            
            return true;
        }

        public async Task<bool> ConvertItemAccessLevelToItemPermissions(int itemType)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            _httpClient.Timeout = TimeSpan.FromMinutes(60);

            string accessApiPath = "/api/Access/ConvertItemAccessLevelToItemPermissions/" + itemType;
            HttpResponseMessage accessManagementResponse = await _httpClient.GetAsync(accessApiPath);

            _httpClient.Timeout = TimeSpan.FromSeconds(100);

            if (!accessManagementResponse.IsSuccessStatusCode) return false;

            return true;
        }
    }
}
