using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.Family;
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
    public class FamiliesHttpClient: IFamiliesHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FamiliesHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, ITokenService tokenService)
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

        public async Task<List<Family>> GetMyFamilies()
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string familiesApiPath = "/api/Families/GetCurrentUsersFamilies";
            HttpResponseMessage familiesResponse = await _httpClient.GetAsync(familiesApiPath);

            if (!familiesResponse.IsSuccessStatusCode) return new List<Family>();
            
            List<Family> families = await familiesResponse.Content.ReadAsAsync<List<Family>>();
            return families;

        }

        public async Task<Family> GetFamily(int familyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string familiesApiPath = "/api/Families/GetFamily/" + familyId;
            HttpResponseMessage familiesResponse = await _httpClient.GetAsync(familiesApiPath);

            if (!familiesResponse.IsSuccessStatusCode) return new Family();
            
            Family family = await familiesResponse.Content.ReadAsAsync<Family>();
            return family;

        }

        public async Task<Family> AddFamily(Family family)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Families", family);

            if (!response.IsSuccessStatusCode) return new Family();
            
            Family newFamily = await response.Content.ReadAsAsync<Family>();
            return newFamily;
        }

        public async Task<Family> UpdateFamily(Family family)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync("/api/Families/" + family.FamilyId, family);
            
            if (!response.IsSuccessStatusCode) return new Family();
            
            Family updatedFamily = await response.Content.ReadAsAsync<Family>();
            return updatedFamily;
        }

        public async Task<bool> DeleteFamily(int familyId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            
            HttpResponseMessage response = await _httpClient.DeleteAsync("/api/Families/" + familyId);
            
            return response.IsSuccessStatusCode;
        }
    }
}
