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
    public class WebNotificationsHttpClient : IWebNotificationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public WebNotificationsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<List<PushDevices>> GetAllPushDevices(bool updateCache = false)
        {
            List<PushDevices> pushDevicesList = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string admininfoApiPath = "/api/Notifications/GetAllPushDevices";
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(admininfoApiPath);
            if (!devicesResponse.IsSuccessStatusCode) return pushDevicesList;
            
            string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
            pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);

            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDeviceById(int id, bool updateCache = false)
        {
            PushDevices pushDevice = new();

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDeviceById/" + id;
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(apiPath);
            if (!httpResponse.IsSuccessStatusCode) return pushDevice;
            
            string deviceAsString = await httpResponse.Content.ReadAsStringAsync();
            pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceAsString);
            return pushDevice;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            PushDevices addedPushDevice = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/AddPushDevice/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedPushDevice;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedPushDevice = JsonConvert.DeserializeObject<PushDevices>(addResponseString);
            return addedPushDevice;
        }

        public async Task<PushDevices> RemovePushDevice(PushDevices device)
        {
            PushDevices deletedPushDevice = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string deleteApiPath = "/api/Notifications/RemovePushDevice/";
            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return deletedPushDevice;

            string deletedResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedPushDevice = JsonConvert.DeserializeObject<PushDevices>(deletedResponseString);
            return deletedPushDevice;
        }

        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string user)
        {
            List<PushDevices> pushDevicesList = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDevicesListByUserId/" + user;
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(apiPath);
            if (!devicesResponse.IsSuccessStatusCode) return pushDevicesList;
            
            string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
            pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);
            return pushDevicesList;
        }

        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices pushDevice = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string apiPath = "/api/Notifications/GetPushDevice/";
            HttpResponseMessage deviceResponse = await _httpClient.PostAsync(apiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!deviceResponse.IsSuccessStatusCode) return pushDevice;

            string deviceResponseString = await deviceResponse.Content.ReadAsStringAsync();
            pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceResponseString);
            return pushDevice;
        }

        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            WebNotification addedNotification = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/AddWebNotification/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedNotification;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedNotification = JsonConvert.DeserializeObject<WebNotification>(addResponseString);
            return addedNotification;
        }

        public async Task<WebNotification> UpdateWebNotification(WebNotification notification)
        {
            WebNotification addedNotification = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/UpdateWebNotification/";
            HttpResponseMessage addResponse = await _httpClient.PutAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedNotification;
            
            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedNotification = JsonConvert.DeserializeObject<WebNotification>(addResponseString);
            return addedNotification;
        }

        public async Task<WebNotification> RemoveWebNotification(WebNotification notification)
        {
            WebNotification removedNotification = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string removeApiPath = "/api/Notifications/RemoveWebNotification/";
            HttpResponseMessage removeResponse = await _httpClient.PostAsync(removeApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!removeResponse.IsSuccessStatusCode) return removedNotification;

            string removeResponseString = await removeResponse.Content.ReadAsStringAsync();
            removedNotification = JsonConvert.DeserializeObject<WebNotification>(removeResponseString);
            return removedNotification;
        }

        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = new();

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetWebNotificationById/" + id;
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(apiPath);
            if (!httpResponse.IsSuccessStatusCode) return notification;
            
            string notificationAsString = await httpResponse.Content.ReadAsStringAsync();
            notification = JsonConvert.DeserializeObject<WebNotification>(notificationAsString);
            return notification;
        }

        public async Task<List<WebNotification>> GetUsersWebNotifications(string user)
        {
            List<WebNotification> usersWebNotifications = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetUsersNotifications/" + user;
            HttpResponseMessage notificationsResponse = await _httpClient.GetAsync(apiPath);
            if (!notificationsResponse.IsSuccessStatusCode) return usersWebNotifications;
            
            string notificationsListAsString = await notificationsResponse.Content.ReadAsStringAsync();
            usersWebNotifications = JsonConvert.DeserializeObject<List<WebNotification>>(notificationsListAsString);
            return usersWebNotifications;
        }
    }
}
