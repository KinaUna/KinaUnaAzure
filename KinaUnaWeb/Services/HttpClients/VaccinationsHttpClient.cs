using IdentityModel.Client;
using KinaUna.Data.Models;
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
    /// Provides methods to interact with the Vaccinations API Controller.
    /// </summary>
    public class VaccinationsHttpClient : IVaccinationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VaccinationsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">The VaccinationId of the Vaccination to get.</param>
        /// <returns>The Vaccination object with the given VaccinationId. If not found, a new Vaccination object with VaccinationId=0 is returned.</returns>
        public async Task<Vaccination> GetVaccination(int vaccinationId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            Vaccination vaccinationItem = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccinationItem ?? new Vaccination();
        }

        /// <summary>
        /// Adds a new Vaccination.
        /// </summary>
        /// <param name="vaccination">The new Vaccination to add.</param>
        /// <returns>The added Vaccination object</returns>
        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string vaccinationsApiPath = "/api/Vaccinations/";
            HttpResponseMessage vaccinationResponse = await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination ?? new Vaccination();
        }

        /// <summary>
        /// Updates a Vaccination. The Vaccination with the same VaccinationId will be updated.
        /// </summary>
        /// <param name="vaccination">The Vaccination object with the updated properties.</param>
        /// <returns>The updated Vaccination. If not found, a new Vaccination object with VaccinationId=0 is returned.</returns>
        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateVaccinationsApiPath = "/api/Vaccinations/" + vaccination.VaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.PutAsync(updateVaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination ?? new Vaccination();
        }

        /// <summary>
        /// Removes the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The VaccinationId of the Vaccination to remove.</param>
        /// <returns>bool: True if the Vaccination was successfully removed.</returns>
        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.DeleteAsync(vaccinationsApiPath);
            return vaccinationResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of all Vaccinations for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Vaccinations for.</param>
        /// <returns>List of Vaccination objects.</returns>
        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> progenyVaccinationsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vaccinationsApiPath = "/api/Vaccinations/Progeny/" + progenyId;
            HttpResponseMessage vaccinationsResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (!vaccinationsResponse.IsSuccessStatusCode) return progenyVaccinationsList;

            string vaccinationsAsString = await vaccinationsResponse.Content.ReadAsStringAsync();
            progenyVaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(vaccinationsAsString);

            return progenyVaccinationsList;
        }

    }
}
