using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.Extensions.Hosting;

namespace KinaUnaWeb.Services
{
    public class ProgenyHttpClient:IProgenyHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;

        public ProgenyHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
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

            string accessToken = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }
        
        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }

            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            Progeny progeny = new Progeny();
            string progenyApiPath = "/api/Progeny/" + progenyId;

            try
            {
                HttpResponseMessage progenyResponse = await _httpClient.GetAsync(progenyApiPath).ConfigureAwait(false);

                if (progenyResponse.IsSuccessStatusCode)
                {
                    string progenyAsString = await progenyResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
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
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string newProgenyApiPath = "/api/Progeny/";
            HttpResponseMessage progenyResponse = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (progenyResponse.IsSuccessStatusCode)
            {
                string newProgeny = await progenyResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Progeny>(newProgeny);
            }

            return new Progeny();
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateProgenyApiPath = "/api/Progeny/" + progeny.Id;
            HttpResponseMessage progenyResponse = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json"));
            if (progenyResponse.IsSuccessStatusCode)
            {
                string updateProgenyResponseString = await progenyResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);
            }

            return new Progeny();
        }

        public async Task<bool> DeleteProgeny(int progenyId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string progenyApiPath = "/api/Progeny/" + progenyId;
            HttpResponseMessage progenyResponse = await _httpClient.DeleteAsync(progenyApiPath);
            if (progenyResponse.IsSuccessStatusCode)
            {
                return true;
            }
            
            return false;
        }

        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/AdminListByUserPost/";
            string id = email;
            List <Progeny> accessList = new List<Progeny>();
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(id), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessResponseString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);
            }
            
            return accessList;
        }
        
        public async Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyPosts = new List<TimeLineItem>();

            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string latestApiPath = "/api/Timeline/ProgenyLatest/" + progenyId + "/" + accessLevel + "/5/0";
            HttpResponseMessage latestResponse = await _httpClient.GetAsync(latestApiPath).ConfigureAwait(false);
            if (latestResponse.IsSuccessStatusCode)
            {
                string latestAsString = await latestResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(latestAsString);
            }

            return progenyPosts;
        }

        public async Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel)
        {
            List<TimeLineItem> yearAgoPosts = new List<TimeLineItem>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string yearAgoApiPath = "/api/Timeline/ProgenyYearAgo/" + progenyId + "/" + accessLevel;
            HttpResponseMessage yearAgoResponse = await _httpClient.GetAsync(yearAgoApiPath).ConfigureAwait(false);
            if (yearAgoResponse.IsSuccessStatusCode)
            {
                string yearAgoAsString = await yearAgoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                yearAgoPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(yearAgoAsString);
            }

            return yearAgoPosts;
        }
    }
}
