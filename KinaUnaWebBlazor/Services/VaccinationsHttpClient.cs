using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class VaccinationsHttpClient: IVaccinationsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public VaccinationsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration.");

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

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration."));
            return accessToken;
        }

        public async Task<Vaccination?> GetVaccination(int vaccinationId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            Vaccination? vaccinationItem = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccinationItem;

        }

        public async Task<Vaccination?> AddVaccination(Vaccination? vaccination)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string vaccinationsApiPath = "/api/Vaccinations/";
            HttpResponseMessage vaccinationResponse = await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination;

        }

        public async Task<Vaccination?> UpdateVaccination(Vaccination? vaccination)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateVaccinationsApiPath = "/api/Vaccinations/" + vaccination?.VaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.PutAsync(updateVaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination;

        }

        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.DeleteAsync(vaccinationsApiPath);
            return vaccinationResponse.IsSuccessStatusCode;
        }

        public async Task<List<Vaccination>?> GetVaccinationsList(int progenyId, int accessLevel)
        {
            List<Vaccination>? progenyVaccinationsList = [];
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage vaccinationsResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (!vaccinationsResponse.IsSuccessStatusCode) return progenyVaccinationsList;

            string vaccinationsAsString = await vaccinationsResponse.Content.ReadAsStringAsync();
            progenyVaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(vaccinationsAsString);

            return progenyVaccinationsList;
        }

    }
}
