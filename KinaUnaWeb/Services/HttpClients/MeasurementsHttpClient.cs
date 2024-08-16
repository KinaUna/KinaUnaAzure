using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Measurements API.
    /// </summary>
    public class MeasurementsHttpClient : IMeasurementsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public MeasurementsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Measurement measurementItem = new();
            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.GetAsync(measurementsApiPath);
            if (!measurementResponse.IsSuccessStatusCode) return measurementItem;

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();

            measurementItem = JsonConvert.DeserializeObject<Measurement>(measurementAsString);

            return measurementItem;
        }

        /// <summary>
        /// Adds a new Measurement. 
        /// </summary>
        /// <param name="measurement">The Measurement object to be added.</param>
        /// <returns>The Measurement object that was added.</returns>
        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string measurementsApiPath = "/api/Measurements/";
            HttpResponseMessage measurementsResponse = await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementsResponse.IsSuccessStatusCode) return new Measurement();

            string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync();
            measurement = JsonConvert.DeserializeObject<Measurement>(measurementsAsString);
            return measurement;

        }

        /// <summary>
        /// Updates a Measurement. The Measurement with the same MeasurementId will be updated.
        /// </summary>
        /// <param name="measurement">The Measurement with the updated properties.</param>
        /// <returns>The updated Measurement object.</returns>
        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateMeasurementsApiPath = "/api/Measurements/" + measurement.MeasurementId;
            HttpResponseMessage measurementResponse = await _httpClient.PutAsync(updateMeasurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementResponse.IsSuccessStatusCode) return new Measurement();

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();
            measurement = JsonConvert.DeserializeObject<Measurement>(measurementAsString);
            return measurement;

        }

        /// <summary>
        /// Removes the Measurement with a given MeasurementId.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to remove.</param>
        /// <returns>bool: True if the Measurement was successfully removed.</returns>
        public async Task<bool> DeleteMeasurement(int measurementId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.DeleteAsync(measurementsApiPath);
            return measurementResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of Measurements for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Measurements for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Measurement objects.</returns>
        public async Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel)
        {
            List<Measurement> progenyMeasurementsList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/measurements/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage measurementsResponse = await _httpClient.GetAsync(measurementsApiPath).ConfigureAwait(false);
            if (!measurementsResponse.IsSuccessStatusCode) return progenyMeasurementsList;

            string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyMeasurementsList = JsonConvert.DeserializeObject<List<Measurement>>(measurementsAsString);

            return progenyMeasurementsList;
        }
    }
}
