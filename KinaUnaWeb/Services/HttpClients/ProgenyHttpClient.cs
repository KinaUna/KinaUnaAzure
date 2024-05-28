using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public class ProgenyHttpClient : IProgenyHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public ProgenyHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }



        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

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

        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string newProgenyApiPath = "/api/Progeny/";
            HttpResponseMessage progenyResponse = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string newProgeny = await progenyResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Progeny>(newProgeny);

        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateProgenyApiPath = "/api/Progeny/" + progeny.Id;
            HttpResponseMessage progenyResponse = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (!progenyResponse.IsSuccessStatusCode) return new Progeny();

            string updateProgenyResponseString = await progenyResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);

        }

        public async Task<bool> DeleteProgeny(int progenyId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string progenyApiPath = "/api/Progeny/" + progenyId;
            HttpResponseMessage progenyResponse = await _httpClient.DeleteAsync(progenyApiPath);
            return progenyResponse.IsSuccessStatusCode;
        }

        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string accessApiPath = "/api/Access/AdminListByUserPost/";
            List<Progeny> accessList = [];
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(email), System.Text.Encoding.UTF8, "application/json"));
            if (!accessResponse.IsSuccessStatusCode) return accessList;

            string accessResponseString = await accessResponse.Content.ReadAsStringAsync();
            accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }

        public async Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyPosts = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string latestApiPath = "/api/Timeline/ProgenyLatest/" + progenyId + "/" + accessLevel + "/5/0";
            HttpResponseMessage latestResponse = await _httpClient.GetAsync(latestApiPath);
            if (!latestResponse.IsSuccessStatusCode) return progenyPosts;

            string latestAsString = await latestResponse.Content.ReadAsStringAsync();

            progenyPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(latestAsString);

            return progenyPosts;
        }

        public async Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel)
        {
            List<TimeLineItem> yearAgoPosts = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string yearAgoApiPath = "/api/Timeline/ProgenyYearAgo/" + progenyId + "/" + accessLevel;
            HttpResponseMessage yearAgoResponse = await _httpClient.GetAsync(yearAgoApiPath);
            if (!yearAgoResponse.IsSuccessStatusCode) return yearAgoPosts;

            string yearAgoAsString = await yearAgoResponse.Content.ReadAsStringAsync();

            yearAgoPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(yearAgoAsString);

            return yearAgoPosts;
        }
    }
}
