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


        public async Task<Measurement> GetMeasurement(int measurementId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Measurement measurementItem = new();
            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.GetAsync(measurementsApiPath);
            if (measurementResponse.IsSuccessStatusCode)
            {
                string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();

                measurementItem = JsonConvert.DeserializeObject<Measurement>(measurementAsString);
            }

            return measurementItem;
        }

        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/Measurements/";
            HttpResponseMessage measurementsResponse = await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (measurementsResponse.IsSuccessStatusCode)
            {
                string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync();
                measurement = JsonConvert.DeserializeObject<Measurement>(measurementsAsString);
                return measurement;
            }

            return new Measurement();
        }

        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateMeasurementsApiPath = "/api/Measurements/" + measurement.MeasurementId;
            HttpResponseMessage measurementResponse = await _httpClient.PutAsync(updateMeasurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (measurementResponse.IsSuccessStatusCode)
            {
                string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();
                measurement = JsonConvert.DeserializeObject<Measurement>(measurementAsString);
                return measurement;
            }

            return new Measurement();
        }

        public async Task<bool> DeleteMeasurement(int measurementId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.DeleteAsync(measurementsApiPath);
            if (measurementResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel)
        {
            List<Measurement> progenyMeasurementsList = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/measurements/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage measurementsResponse = await _httpClient.GetAsync(measurementsApiPath).ConfigureAwait(false);
            if (measurementsResponse.IsSuccessStatusCode)
            {
                string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyMeasurementsList = JsonConvert.DeserializeObject<List<Measurement>>(measurementsAsString);
            }

            return progenyMeasurementsList;
        }
    }
}
