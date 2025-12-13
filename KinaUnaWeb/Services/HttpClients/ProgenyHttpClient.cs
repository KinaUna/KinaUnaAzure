using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using System.Text.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Progeny API.
    /// Contains the methods for adding, retrieving and updating progeny and user data.
    /// </summary>
    public class ProgenyHttpClient : IProgenyHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProgenyHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHostEnvironment env, IHttpContextAccessor httpContextAccessor)
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
        /// Gets the Progeny with the given Id.
        /// </summary>
        /// <param name="progenyId">The Progeny's Id.</param>
        /// <returns>Progeny object with the given Id. If not found, a new Progeny object with Id=0 is returned.</returns>
        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            Progeny progeny = new();
            string progenyApiPath = "/api/Progeny/" + progenyId;

            try
            {
                HttpResponseMessage progenyResponse = await _httpClient.GetAsync(progenyApiPath);

                if (progenyResponse.IsSuccessStatusCode)
                {
                    string progenyAsString = await progenyResponse.Content.ReadAsStringAsync();
                    progeny = JsonSerializer.Deserialize<Progeny>(progenyAsString, JsonSerializerOptions.Web);
                }
                else
                {
                    progeny.Name = "401";

                }
            }
            catch (Exception e)
            {
                if (progeny != null)
                {
                    progeny.Name = "401";
                    progeny.NickName = e.Message;
                    return progeny;
                }
            }

            return progeny;
        }

        /// <summary>
        /// Retrieves a list of progenies that the currently signed-in user can access based on the specified permission
        /// level.
        /// </summary>
        /// <remarks>This method uses the currently signed-in user's identity to determine access. The
        /// user must be authenticated, and their access token must be valid.</remarks>
        /// <param name="permissionLevel">The level of permission required to access the progenies. This determines which progenies are included in
        /// the result.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Progeny"/>
        /// objects that the user has access to. If no progenies are accessible, an empty list is returned.</returns>
        public async Task<List<Progeny>> GetProgeniesUserCanAccess(PermissionLevel permissionLevel)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            string accessManagementPath = "/api/AccessManagement/ProgeniesUserCanAccessList/" + (int)permissionLevel;
            HttpResponseMessage progeniesResponse = await _httpClient.GetAsync(accessManagementPath);

            if (!progeniesResponse.IsSuccessStatusCode) return new List<Progeny>();

            List<Progeny> progenies = await progeniesResponse.Content.ReadAsAsync<List<Progeny>>();
            return progenies;
        }

        /// <summary>
        /// Retrieves the list of permissions associated with a specific progeny.
        /// </summary>
        /// <remarks>This method makes an HTTP request to an external service to retrieve the permissions.
        /// Ensure that the signed-in user has a valid token, as it is required for authentication.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose permissions are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="ProgenyPermission"/> objects representing the permissions for the specified progeny. Returns an empty
        /// list if no permissions are found or if the request fails.</returns>
        public async Task<List<ProgenyPermission>> GetProgenyPermissionsList(int progenyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            string accessManagementPath = "/api/AccessManagement/GetProgenyPermissionsList/" + progenyId;
            HttpResponseMessage permissionsResponse = await _httpClient.GetAsync(accessManagementPath);
            if (!permissionsResponse.IsSuccessStatusCode) return new List<ProgenyPermission>();
            List<ProgenyPermission> permissions = await permissionsResponse.Content.ReadAsAsync<List<ProgenyPermission>>();
            return permissions;
        }

        /// <summary>
        /// Adds a new Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object to be added.</param>
        /// <returns>Progeny: The Progeny object that was added.</returns>
        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string newProgenyApiPath = "/api/Progeny/";
            HttpResponseMessage progenyResponse = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonSerializer.Serialize(progeny, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string newProgeny = await progenyResponse.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Progeny>(newProgeny, JsonSerializerOptions.Web);

        }

        /// <summary>
        /// Updates a Progeny.
        /// </summary>
        /// <param name="progeny">The Progeny object with the updated properties.</param>
        /// <returns>The updated Progeny object.</returns>
        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateProgenyApiPath = "/api/Progeny/" + progeny.Id;
            HttpResponseMessage progenyResponse = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonSerializer.Serialize(progeny, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string updateProgenyResponseString = await progenyResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Progeny>(updateProgenyResponseString, JsonSerializerOptions.Web);

        }

        /// <summary>
        /// Removes a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to be removed.</param>
        /// <returns>bool: True if successfully removed.</returns>
        public async Task<bool> DeleteProgeny(int progenyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string progenyApiPath = "/api/Progeny/" + progenyId;
            HttpResponseMessage progenyResponse = await _httpClient.DeleteAsync(progenyApiPath);
            return progenyResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the ProgenyInfo object for the given Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get the ProgenyInfo object for.</param>
        /// <returns>The ProgenyInfo object for the given Progeny.</returns>
        public async Task<ProgenyInfo> GetProgenyInfo(int progenyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            ProgenyInfo progenyInfo = new();
            string progenyInfoApiPath = "/api/Progeny/GetProgenyInfo/" + progenyId;

            HttpResponseMessage progenyInfoResponse = await _httpClient.GetAsync(progenyInfoApiPath);

            if (!progenyInfoResponse.IsSuccessStatusCode) return progenyInfo;

            string progenyInfoAsString = await progenyInfoResponse.Content.ReadAsStringAsync();
            progenyInfo = JsonSerializer.Deserialize<ProgenyInfo>(progenyInfoAsString, JsonSerializerOptions.Web);

            return progenyInfo;

        }

        /// <summary>
        /// Updates a ProgenyInfo object.
        /// </summary>
        /// <param name="progenyInfo">The ProgenyInfo object with the updated properties.</param>
        /// <returns>The updated ProgenyInfo object.</returns>
        public async Task<ProgenyInfo> UpdateProgenyInfo(ProgenyInfo progenyInfo)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateProgenyInfoApiPath = "/api/Progeny/UpdateProgenyInfo/" + progenyInfo.ProgenyId;
            HttpResponseMessage progenyInfoResponse = await _httpClient.PutAsync(updateProgenyInfoApiPath, new StringContent(JsonSerializer.Serialize(progenyInfo, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyInfoResponse.IsSuccessStatusCode) return new ProgenyInfo();

            string updateProgenyInfoResponseString = await progenyInfoResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProgenyInfo>(updateProgenyInfoResponseString, JsonSerializerOptions.Web);

        }

        /// <summary>
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string accessApiPath = "/api/Access/AdminListByUserPost/";
            List<Progeny> accessList = [];
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonSerializer.Serialize(email, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessResponseString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonSerializer.Deserialize<List<Progeny>>(accessResponseString, JsonSerializerOptions.Web);

            return accessList;
        }

        /// <summary>
        /// Retrieves the permission details for a specific progeny permission based on the provided permission ID.
        /// </summary>
        /// <remarks>This method retrieves the permission details by making an HTTP request to the access
        /// management API. Ensure that the signed-in user has a valid token, as the request requires
        /// authentication.</remarks>
        /// <param name="permissionId">The unique identifier of the permission to retrieve.</param>
        /// <returns>A <see cref="ProgenyPermission"/> object containing the details of the specified permission.  If the
        /// permission is not found or the request fails, an empty <see cref="ProgenyPermission"/> object is returned.</returns>
        public async Task<ProgenyPermission> GetProgenyPermission(int permissionId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string accessManagementPath = "/api/AccessManagement/GetProgenyPermission/" + permissionId;
            HttpResponseMessage permissionResponse = await _httpClient.GetAsync(accessManagementPath);
            if (!permissionResponse.IsSuccessStatusCode) return new ProgenyPermission();

            ProgenyPermission permission = await permissionResponse.Content.ReadAsAsync<ProgenyPermission>();
            return permission;
        }

        /// <summary>
        /// Adds a new progeny permission by sending the specified <see cref="ProgenyPermission"/> object to the access
        /// management API.
        /// </summary>
        /// <remarks>This method requires the user to be authenticated, as it retrieves the signed-in
        /// user's token to authorize the request. Ensure that the <paramref name="progenyPermission"/> parameter is
        /// properly populated before calling this method.</remarks>
        /// <param name="progenyPermission">The <see cref="ProgenyPermission"/> object containing the details of the progeny permission to be added.</param>
        /// <returns>A <see cref="ProgenyPermission"/> object representing the added progeny permission, as returned by the API. If
        /// the operation fails, an empty <see cref="ProgenyPermission"/> object is returned.</returns>
        public async Task<ProgenyPermission> AddProgenyPermission(ProgenyPermission progenyPermission)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string accessApiPath = "/api/AccessManagement/AddProgenyPermission/";
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonSerializer.Serialize(progenyPermission, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return new ProgenyPermission();
            
            string accessResponseString = await accessResponse.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<ProgenyPermission>(accessResponseString, JsonSerializerOptions.Web);
        }

        /// <summary>
        /// Updates the specified progeny permission in the system.
        /// </summary>
        /// <remarks>This method sends an HTTP PUT request to the access management API to update the
        /// specified progeny permission.  Ensure that the <paramref name="progenyPermission"/> parameter contains valid
        /// data, including a valid <see cref="ProgenyPermission.ProgenyPermissionId"/>.</remarks>
        /// <param name="progenyPermission">The <see cref="ProgenyPermission"/> object containing the updated permission details. The <see
        /// cref="ProgenyPermission.ProgenyPermissionId"/> property must be set to identify the permission to update.</param>
        /// <returns>A <see cref="ProgenyPermission"/> object representing the updated permission if the operation is successful;
        /// otherwise, a new <see cref="ProgenyPermission"/> object with default values.</returns>
        public async Task<ProgenyPermission> UpdateProgenyPermission(ProgenyPermission progenyPermission)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            string updateProgenyPermissionApiPath = "/api/AccessManagement/UpdateProgenyPermission/" + progenyPermission.ProgenyPermissionId;
            HttpResponseMessage accessResponse = await _httpClient.PutAsync(updateProgenyPermissionApiPath, new StringContent(JsonSerializer.Serialize(progenyPermission, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return new ProgenyPermission();
            string updateProgenyPermissionResponseString = await accessResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProgenyPermission>(updateProgenyPermissionResponseString, JsonSerializerOptions.Web);
        }

        /// <summary>
        /// Deletes a progeny permission with the specified permission ID.
        /// </summary>
        /// <remarks>This method sends an HTTP DELETE request to the access management API to remove the
        /// specified progeny permission. The caller must ensure that the <paramref name="permissionId"/> corresponds to
        /// a valid permission.</remarks>
        /// <param name="permissionId">The unique identifier of the permission to delete.</param>
        /// <returns><see langword="true"/> if the permission was successfully deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteProgenyPermission(int permissionId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            string accessManagementPath = "/api/AccessManagement/DeleteProgenyPermission/" + permissionId;
            HttpResponseMessage permissionResponse = await _httpClient.DeleteAsync(accessManagementPath);
            return permissionResponse.IsSuccessStatusCode;
        }
    }
}
