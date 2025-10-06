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
                    progeny = JsonConvert.DeserializeObject<Progeny>(progenyAsString);
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
            
            string accessManagementPath = "/api/AccessManagement/ProgeniesUserCanAccessList/" + permissionLevel;
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
            HttpResponseMessage progenyResponse = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string newProgeny = await progenyResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Progeny>(newProgeny);

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
            HttpResponseMessage progenyResponse = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string updateProgenyResponseString = await progenyResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);

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
            progenyInfo = JsonConvert.DeserializeObject<ProgenyInfo>(progenyInfoAsString);

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
            HttpResponseMessage progenyInfoResponse = await _httpClient.PutAsync(updateProgenyInfoApiPath, new StringContent(JsonConvert.SerializeObject(progenyInfo), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyInfoResponse.IsSuccessStatusCode) return new ProgenyInfo();

            string updateProgenyInfoResponseString = await progenyInfoResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ProgenyInfo>(updateProgenyInfoResponseString);

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
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessResponseString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }
    }
}
