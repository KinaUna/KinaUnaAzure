using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class MeasurementsHttpClient: IMeasurementsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public MeasurementsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext? currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration"));
            return accessToken;
        }

        public async Task<Measurement?> GetMeasurement(int measurementId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            Measurement? measurementItem = new();
            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.GetAsync(measurementsApiPath);
            if (!measurementResponse.IsSuccessStatusCode) return measurementItem;

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();

            measurementItem = JsonConvert.DeserializeObject<Measurement>(measurementAsString);

            return measurementItem;
        }

        public async Task<Measurement?> AddMeasurement(Measurement? measurement)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string measurementsApiPath = "/api/Measurements/";
            HttpResponseMessage measurementsResponse = await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementsResponse.IsSuccessStatusCode) return new Measurement();

            string measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync();
            measurement = JsonConvert.DeserializeObject<Measurement>(measurementsAsString);
            return measurement;

        }

        public async Task<Measurement?> UpdateMeasurement(Measurement? measurement)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateMeasurementsApiPath = "/api/Measurements/" + measurement?.MeasurementId;
            HttpResponseMessage measurementResponse = await _httpClient.PutAsync(updateMeasurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json"));
            if (!measurementResponse.IsSuccessStatusCode) return new Measurement();

            string measurementAsString = await measurementResponse.Content.ReadAsStringAsync();
            measurement = JsonConvert.DeserializeObject<Measurement>(measurementAsString);
            return measurement;

        }

        public async Task<bool> DeleteMeasurement(int measurementId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string measurementsApiPath = "/api/Measurements/" + measurementId;
            HttpResponseMessage measurementResponse = await _httpClient.DeleteAsync(measurementsApiPath);
            return measurementResponse.IsSuccessStatusCode;
        }

        public async Task<List<Measurement>?> GetMeasurementsList(int progenyId, int accessLevel)
        {
            List<Measurement>? progenyMeasurementsList = [];
            string accessToken = await GetNewToken();
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
