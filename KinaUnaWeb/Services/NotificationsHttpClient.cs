using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class NotificationsHttpClient : INotificationsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;
        
        public NotificationsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
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

            httpClient.BaseAddress = new Uri(clientUri!);
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

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<List<PushDevices>> GetAllPushDevices(bool updateCache = false)
        {
            List<PushDevices> pushDevicesList = new List<PushDevices>();
            
            string accessToken = await GetNewToken(true);
            _httpClient.SetBearerToken(accessToken);

            string admininfoApiPath = "/api/Notifications/GetAllPushDevices";
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(admininfoApiPath);

            if (devicesResponse.IsSuccessStatusCode)
            {
                string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
                pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);
            }

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDeviceById(int id, bool updateCache = false)
        {
            PushDevices pushDevice = new PushDevices();
            
            string accessToken = await GetNewToken(true);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDeviceById/" + id;
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(apiPath);

            if (httpResponse.IsSuccessStatusCode)
            {
                string deviceAsString = await httpResponse.Content.ReadAsStringAsync();
                pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceAsString);
            }

            return pushDevice;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            PushDevices addedPushDevice = new PushDevices();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string addApiPath = "/api/Notifications/AddPushDevice/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                string addResponseString = await addResponse.Content.ReadAsStringAsync();
                addedPushDevice = JsonConvert.DeserializeObject<PushDevices>(addResponseString);
            }

            return addedPushDevice;
        }
        
        public async Task<PushDevices> RemovePushDevice(PushDevices device)
        {
            PushDevices deletedPushDevice = new PushDevices();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string deleteApiPath = "/api/Notifications/RemovePushDevice/";
            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (deleteResponse.IsSuccessStatusCode)
            {
                string deletedResponseString = await deleteResponse.Content.ReadAsStringAsync();
                deletedPushDevice = JsonConvert.DeserializeObject<PushDevices>(deletedResponseString);
            }

            return deletedPushDevice;
        }

        public async Task<List<PushDevices>> GetPushDeviceByUserId(string user)
        {
            List<PushDevices> pushDevicesList = new List<PushDevices>();
           
            string accessToken = await GetNewToken(true);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDeviceByUserId/" + user;
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(apiPath);

            if (devicesResponse.IsSuccessStatusCode)
            {
                string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
                pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);
            }

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices pushDevice = new PushDevices();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDevice/";
            HttpResponseMessage deviceResponse = await _httpClient.PostAsync(apiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (deviceResponse.IsSuccessStatusCode)
            {
                string deviceResponseString = await deviceResponse.Content.ReadAsStringAsync();
                pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceResponseString);
            }

            return pushDevice;
        }
    }
}
