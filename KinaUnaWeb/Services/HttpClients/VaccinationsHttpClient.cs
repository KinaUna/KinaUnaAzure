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
    public class VaccinationsHttpClient : IVaccinationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public VaccinationsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<Vaccination> GetVaccination(int vaccinationId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.GetAsync(vaccinationsApiPath);
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            Vaccination vaccinationItem = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccinationItem ?? new Vaccination();
        }

        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string vaccinationsApiPath = "/api/Vaccinations/";
            HttpResponseMessage vaccinationResponse = await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination ?? new Vaccination();
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateVaccinationsApiPath = "/api/Vaccinations/" + vaccination.VaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.PutAsync(updateVaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json"));
            if (!vaccinationResponse.IsSuccessStatusCode) return new Vaccination();

            string vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync();
            vaccination = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            return vaccination ?? new Vaccination();
        }

        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string vaccinationsApiPath = "/api/Vaccinations/" + vaccinationId;
            HttpResponseMessage vaccinationResponse = await _httpClient.DeleteAsync(vaccinationsApiPath);
            return vaccinationResponse.IsSuccessStatusCode;
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel)
        {
            List<Vaccination> progenyVaccinationsList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
