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
    public class UserGroupsHttpClient : IUserGroupsHttpClient
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

        /// <summary>
        /// Retrieves the details of a user group by its unique identifier.
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. The method retrieves a valid
        /// access token  for the signed-in user and includes it in the request to the user groups API.</remarks>
        /// <param name="userGroupId">The unique identifier of the user group to retrieve.</param>
        /// <returns>A <see cref="UserGroup"/> object containing the details of the specified user group.  If the user group is
        /// not found or the request fails, an empty <see cref="UserGroup"/> object is returned.</returns>
        public async Task<UserGroup> GetUserGroup(int userGroupId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/GetUserGroup/" + userGroupId;
            HttpResponseMessage response = await _httpClient.GetAsync(userGroupsApiPath);
            if (!response.IsSuccessStatusCode)
            {
                return new UserGroup();
            }

            UserGroup userGroup = await response.Content.ReadAsAsync<UserGroup>();
            
            return userGroup;
        }

        /// <summary>
        /// Adds a new user group.
        /// </summary>
        /// <param name="userGroup">The user group to add</param>
        /// <returns>The added user group</returns>
        public async Task<UserGroup> AddUserGroup(UserGroup userGroup)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/";
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(userGroupsApiPath, userGroup);
            if (!response.IsSuccessStatusCode)
            {
                return new UserGroup();
            }

            UserGroup addedUserGroup = await response.Content.ReadAsAsync<UserGroup>();

            return addedUserGroup;
        }

        /// <summary>
        /// Updates an existing user group.
        /// </summary>
        /// <param name="userGroup">The user group to update</param>
        /// <returns>The updated user group</returns>
        public async Task<UserGroup> UpdateUserGroup(UserGroup userGroup)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/";
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync(userGroupsApiPath, userGroup);
            if (!response.IsSuccessStatusCode)
            {
                return new UserGroup();
            }

            UserGroup updatedUserGroup = await response.Content.ReadAsAsync<UserGroup>();

            return updatedUserGroup;
        }

        /// <summary>
        /// Deletes a user group with the specified identifier.
        /// </summary>
        /// <remarks>This method sends an HTTP DELETE request to the user groups API to remove the
        /// specified user group. The caller must ensure that the <paramref name="userGroupId"/> corresponds to a valid
        /// user group.</remarks>
        /// <param name="userGroupId">The unique identifier of the user group to delete.</param>
        /// <returns><see langword="true"/> if the user group was successfully deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteUserGroup(int userGroupId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/" + userGroupId;
            HttpResponseMessage response = await _httpClient.DeleteAsync(userGroupsApiPath);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a new user to a user group.
        /// </summary>
        /// <param name="userGroupMember">The user group member to add</param>
        /// <returns>The added user group member</returns>
        public async Task<UserGroupMember> AddUserGroupMember(UserGroupMember userGroupMember)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            string userGroupsApiPath = "api/UserGroups/AddUserGroupMember";

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(userGroupsApiPath, userGroupMember);
            if (!response.IsSuccessStatusCode)
            {
                return new UserGroupMember();
            }

            UserGroupMember addedUserGroupMember = await response.Content.ReadAsAsync<UserGroupMember>();

            return addedUserGroupMember;
        }

        /// <summary>
        /// Updates the details of an existing user group member.
        /// </summary>
        /// <remarks>This method sends an HTTP PUT request to the User Groups API to update the specified
        /// user group member. The caller must ensure that the <paramref name="userGroupMember"/> parameter contains
        /// valid data.</remarks>
        /// <param name="userGroupMember">The <see cref="UserGroupMember"/> object containing the updated details of the user group member.</param>
        /// <returns>A <see cref="UserGroupMember"/> object representing the updated user group member.  If the update operation
        /// fails, an empty <see cref="UserGroupMember"/> object is returned.</returns>
        public async Task<UserGroupMember> UpdateUserGroupMember(UserGroupMember userGroupMember)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string userGroupsApiPath = "api/UserGroups/UpdateUserGroupMember";
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync(userGroupsApiPath, userGroupMember);
            if (!response.IsSuccessStatusCode)
            {
                return new UserGroupMember();
            }

            UserGroupMember updatedUserGroupMember = await response.Content.ReadAsAsync<UserGroupMember>();

            return updatedUserGroupMember;
        }

        /// <summary>
        /// Removes a user group member with the specified identifier.
        /// </summary>
        /// <remarks>This method sends a DELETE request to the User Groups API to remove the specified
        /// user group member. The operation requires a valid access token, which is retrieved for the currently
        /// signed-in user.</remarks>
        /// <param name="userGroupMemberId">The unique identifier of the user group member to be removed.</param>
        /// <returns><see langword="true"/> if the user group member was successfully removed; otherwise, <see
        /// langword="false"/>.</returns>
        public async Task<bool> RemoveUserGroupMember(int userGroupMemberId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            string userGroupsApiPath = "api/UserGroups/RemoveUserGroupMember/" + userGroupMemberId;
            HttpResponseMessage response = await _httpClient.DeleteAsync(userGroupsApiPath);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }
    }
}
