using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class VaccinationsHttpClient: IVaccinationsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;

        public VaccinationsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _env = env;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
            }

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName, _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<Vaccination> GetVaccination(int vaccinationId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (vaccinationResponse.IsSuccessStatusCode)
            {
                string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
                Vaccination vaccinationItem = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
                if (vaccinationItem != null)
                {
                    return vaccinationItem;
                }
            }

            return new Vaccination();
        }

        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/";
            HttpResponseMessage vaccinationResponse = await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (vaccinationResponse.IsSuccessStatusCode)
            {
                string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
                vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
                if (vaccination != null)
                {
                    return vaccination;
                }
            }

            return new Vaccination();
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateVaccinationsApiPath = "/api/Vaccinations/" + vaccination.VaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.PutAsync(updateVaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (vaccinationResponse.IsSuccessStatusCode)
            {
                string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
                vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
                if (vaccination != null)
                {
                    return vaccination;
                }
            }

            return new Vaccination();
        }

        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.DeleteAsync(vaccinationsApiPath);
            if (vaccinationResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel)
        {
            List<Vaccination> progenyVaccinationsList = new List<Vaccination>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage vaccinationsResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (vaccinationsResponse.IsSuccessStatusCode)
            {
                string vaccinationsAsString = await vaccinationsResponse.Content.ReadAsStringAsync();
                progenyVaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(vaccinationsAsString);
            }

            return progenyVaccinationsList;
        }

    }
}
