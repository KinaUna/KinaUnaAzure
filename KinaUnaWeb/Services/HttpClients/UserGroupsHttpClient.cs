using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the User Groups API to retrieve user group information for families and
    /// progeny. This client handles authentication and communication with the API.
    /// </summary>
    /// <remarks>This class is designed to be used in applications that require access to user group data from
    /// an external API. It manages HTTP requests, including setting authentication tokens and handling API responses.
    /// The base address for the API is configured based on the application's environment (e.g., Development,
    /// Staging).</remarks>
    public class UserGroupsHttpClient: IUserGroupsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserGroupsHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, ITokenService tokenService)
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
        /// Gets the list of user groups for a specific family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family.</param>
        /// <returns>A list of <see cref="UserGroup"/> objects associated with the specified family. If no groups are found or an error occurs, an empty list is returned.</returns>
        public async Task<List<UserGroup>> GetUserGroupsForFamily(int familyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/GetUserGroupsForFamily/" + familyId;
            HttpResponseMessage response = await _httpClient.GetAsync(userGroupsApiPath);
            if (!response.IsSuccessStatusCode)
            {
                return new List<UserGroup>();
            }
            
            List<UserGroup> userGroups = await response.Content.ReadAsAsync<List<UserGroup>>();
            
            return userGroups;
        }

        /// <summary>
        /// Gets the list of user groups for a specific progeny.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <returns>A list of <see cref="UserGroup"/> objects associated with the specified progeny. If no groups are found or an error occurs, an empty list is returned.</returns>
        public async Task<List<UserGroup>> GetUserGroupsForProgeny(int progenyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            string userGroupsApiPath = "api/UserGroups/GetUserGroupsForProgeny/" + progenyId;
            
            HttpResponseMessage response = await _httpClient.GetAsync(userGroupsApiPath);
            if (!response.IsSuccessStatusCode)
            {
                return new List<UserGroup>();
            }
            
            List<UserGroup> userGroups = await response.Content.ReadAsAsync<List<UserGroup>>();
            
            return userGroups;
        }
    }
}
