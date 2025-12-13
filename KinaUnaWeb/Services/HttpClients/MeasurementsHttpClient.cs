using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Measurements API.
    /// </summary>
    public class MeasurementsHttpClient : IMeasurementsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MeasurementsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
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
        /// Gets the Measurement with the given MeasurementId.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to get.</param>
        /// <returns>The Measurement object with the given MeasurementId.</returns>
        public async Task<Measurement> GetMeasurement(int measurementId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            Measurement measurementItem = new();
            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.GetAsync(measurementsApiPath);
            if (!measurementResponse.IsSuccessStatusCode) return measurementItem;

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();

            measurementItem = JsonSerializer.Deserialize<Measurement>(measurementAsString, JsonSerializerOptions.Web);

            return measurementItem;
        }

        /// <summary>
        /// Adds a new Measurement. 
        /// </summary>
        /// <param name="measurement">The Measurement object to be added.</param>
        /// <returns>The Measurement object that was added.</returns>
        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string measurementsApiPath = "/api/Measurements/";
            HttpResponseMessage measurementsResponse = await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonSerializer.Serialize(measurement, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementsResponse.IsSuccessStatusCode) return new Measurement();

            string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync();
            measurement = JsonSerializer.Deserialize<Measurement>(measurementsAsString, JsonSerializerOptions.Web);
            return measurement;

        }

        /// <summary>
        /// Updates a Measurement. The Measurement with the same MeasurementId will be updated.
        /// </summary>
        /// <param name="measurement">The Measurement with the updated properties.</param>
        /// <returns>The updated Measurement object.</returns>
        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateMeasurementsApiPath = "/api/Measurements/" + measurement.MeasurementId;
            HttpResponseMessage measurementResponse = await _httpClient.PutAsync(updateMeasurementsApiPath, new StringContent(JsonSerializer.Serialize(measurement, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementResponse.IsSuccessStatusCode) return new Measurement();

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();
            measurement = JsonSerializer.Deserialize<Measurement>(measurementAsString, JsonSerializerOptions.Web);
            return measurement;

        }

        /// <summary>
        /// Removes the Measurement with a given MeasurementId.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to remove.</param>
        /// <returns>bool: True if the Measurement was successfully removed.</returns>
        public async Task<bool> DeleteMeasurement(int measurementId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.DeleteAsync(measurementsApiPath);
            return measurementResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of Measurements for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Measurements for.</param>
        /// <returns>List of Measurement objects.</returns>
        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> progenyMeasurementsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string measurementsApiPath = "/api/measurements/progeny/" + progenyId;
            HttpResponseMessage measurementsResponse = await _httpClient.GetAsync(measurementsApiPath);
            if (!measurementsResponse.IsSuccessStatusCode) return progenyMeasurementsList;

            string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync();

            progenyMeasurementsList = JsonSerializer.Deserialize<List<Measurement>>(measurementsAsString, JsonSerializerOptions.Web);

            return progenyMeasurementsList;
        }
    }
}
