using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Notifications API Controller.
    /// </summary>
    public class WebNotificationsHttpClient : IWebNotificationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public WebNotificationsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets all PushDevices.
        /// </summary>
        /// <returns>List of PushDevices objects.</returns>
        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = [];

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string admininfoApiPath = "/api/Notifications/GetAllPushDevices";
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(admininfoApiPath);
            if (!devicesResponse.IsSuccessStatusCode) return pushDevicesList;
            
            string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
            pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);

            return pushDevicesList;
        }

        /// <summary>
        /// Gets a PushDevices by Id.
        /// </summary>
        /// <param name="id">The Id of the PushDevices to get.</param>
        /// <returns>The PushDevices object with the given Id. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices pushDevice = new();

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDeviceById/" + id;
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(apiPath);
            if (!httpResponse.IsSuccessStatusCode) return pushDevice;
            
            string deviceAsString = await httpResponse.Content.ReadAsStringAsync();
            pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceAsString);
            return pushDevice;
        }

        /// <summary>
        /// Adds a new PushDevices.
        /// </summary>
        /// <param name="device">The PushDevices to add.</param>
        /// <returns>The added PushDevices object. If an error occurs, return a new PushDevices with Id=0.</returns>
        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            PushDevices addedPushDevice = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/AddPushDevice/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedPushDevice;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedPushDevice = JsonConvert.DeserializeObject<PushDevices>(addResponseString);
            return addedPushDevice;
        }

        /// <summary>
        /// Removes a PushDevices.
        /// </summary>
        /// <param name="device">The PushDevices object to remove.</param>
        /// <returns>The removed PushDevices object. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
        public async Task<PushDevices> RemovePushDevice(PushDevices device)
        {
            PushDevices deletedPushDevice = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string deleteApiPath = "/api/Notifications/RemovePushDevice/";
            HttpResponseMessage deleteResponse = await _httpClient.PostAsync(deleteApiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!deleteResponse.IsSuccessStatusCode) return deletedPushDevice;

            string deletedResponseString = await deleteResponse.Content.ReadAsStringAsync();
            deletedPushDevice = JsonConvert.DeserializeObject<PushDevices>(deletedResponseString);
            return deletedPushDevice;
        }

        /// <summary>
        /// Get the list of all PushDevices for a given user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>List of PushDevices objects.</returns>
        public async Task<List<PushDevices>> GetPushDevicesListByUserId(string userId)
        {
            List<PushDevices> pushDevicesList = [];

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetPushDevicesListByUserId/" + userId;
            HttpResponseMessage devicesResponse = await _httpClient.GetAsync(apiPath);
            if (!devicesResponse.IsSuccessStatusCode) return pushDevicesList;
            
            string deviceListAsString = await devicesResponse.Content.ReadAsStringAsync();
            pushDevicesList = JsonConvert.DeserializeObject<List<PushDevices>>(deviceListAsString);
            return pushDevicesList;
        }

        /// <summary>
        /// Gets a PushDevices by the PushDevices' Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">The PushDevices object to get.</param>
        /// <returns>PushDevices object with the provided properties. Null if the item isn't found. If an error occurs a new PushDevices object with Id=0.</returns>
        public async Task<PushDevices> GetPushDevice(PushDevices device)
        {
            PushDevices pushDevice = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string apiPath = "/api/Notifications/GetPushDevice/";
            HttpResponseMessage deviceResponse = await _httpClient.PostAsync(apiPath, new StringContent(JsonConvert.SerializeObject(device), System.Text.Encoding.UTF8, "application/json"));
            if (!deviceResponse.IsSuccessStatusCode) return pushDevice;

            string deviceResponseString = await deviceResponse.Content.ReadAsStringAsync();
            pushDevice = JsonConvert.DeserializeObject<PushDevices>(deviceResponseString);
            return pushDevice;
        }

        /// <summary>
        /// Adds a new WebNotification.
        /// </summary>
        /// <param name="notification">The WebNotification to add.</param>
        /// <returns>The added WebNotification object. If an error occurs a new WebNotification with Id=0 is returned.</returns>
        public async Task<WebNotification> AddWebNotification(WebNotification notification)
        {
            WebNotification addedNotification = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/AddWebNotification/";
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedNotification;

            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedNotification = JsonConvert.DeserializeObject<WebNotification>(addResponseString);
            return addedNotification;
        }

        /// <summary>
        /// Updates a WebNotification.
        /// </summary>
        /// <param name="notification">The WebNotification with the updated properties.</param>
        /// <returns>The updated WebNotification. If not found or an error occurs, a new WebNotification with Id=0 is returned.</returns>
        public async Task<WebNotification> UpdateWebNotification(WebNotification notification)
        {
            WebNotification addedNotification = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string addApiPath = "/api/Notifications/UpdateWebNotification/";
            HttpResponseMessage addResponse = await _httpClient.PutAsync(addApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!addResponse.IsSuccessStatusCode) return addedNotification;
            
            string addResponseString = await addResponse.Content.ReadAsStringAsync();
            addedNotification = JsonConvert.DeserializeObject<WebNotification>(addResponseString);
            return addedNotification;
        }

        /// <summary>
        /// Removes a WebNotification.
        /// </summary>
        /// <param name="notification">The WebNotification to remove.</param>
        /// <returns>The deleted WebNotification. If not found or an error occurs, a new WebNotification with Id=0 is returned.</returns>
        public async Task<WebNotification> RemoveWebNotification(WebNotification notification)
        {
            WebNotification removedNotification = new();
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string removeApiPath = "/api/Notifications/RemoveWebNotification/";
            HttpResponseMessage removeResponse = await _httpClient.PostAsync(removeApiPath, new StringContent(JsonConvert.SerializeObject(notification), System.Text.Encoding.UTF8, "application/json"));
            if (!removeResponse.IsSuccessStatusCode) return removedNotification;

            string removeResponseString = await removeResponse.Content.ReadAsStringAsync();
            removedNotification = JsonConvert.DeserializeObject<WebNotification>(removeResponseString);
            return removedNotification;
        }

        /// <summary>
        /// Gets a WebNotification by Id.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. If the item cannot be found or an error occurs a new WebNotification with Id=0 is returned.</returns>
        public async Task<WebNotification> GetWebNotificationById(int id)
        {
            WebNotification notification = new();

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetWebNotificationById/" + id;
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(apiPath);
            if (!httpResponse.IsSuccessStatusCode) return notification;
            
            string notificationAsString = await httpResponse.Content.ReadAsStringAsync();
            notification = JsonConvert.DeserializeObject<WebNotification>(notificationAsString);
            return notification;
        }

        /// <summary>
        /// Gets the list of all WebNotifications for a given user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>List of WebNotification objects.</returns>
        public async Task<List<WebNotification>> GetUsersWebNotifications(string userId)
        {
            List<WebNotification> usersWebNotifications = [];

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetUsersNotifications/" + userId;
            HttpResponseMessage notificationsResponse = await _httpClient.GetAsync(apiPath);
            if (!notificationsResponse.IsSuccessStatusCode) return usersWebNotifications;
            
            string notificationsListAsString = await notificationsResponse.Content.ReadAsStringAsync();
            usersWebNotifications = JsonConvert.DeserializeObject<List<WebNotification>>(notificationsListAsString);
            return usersWebNotifications;
        }

        /// <summary>
        /// Gets the latest WebNotifications for a given user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <param name="start">Number of WebNotifications to skip.</param>
        /// <param name="count">Number of WebNotifications to get.</param>
        /// <param name="unreadOnly">Include unread WebNotifications only.</param>
        /// <returns>List of WebNotification objects.</returns>
        public async Task<List<WebNotification>> GetLatestWebNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true)
        {
            List<WebNotification> usersWebNotifications = [];

            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetLatestWebNotifications/" + userId + "/" + start + "/" + count + "/" + unreadOnly;
            HttpResponseMessage notificationsResponse = await _httpClient.GetAsync(apiPath);
            if (!notificationsResponse.IsSuccessStatusCode) return usersWebNotifications;
            
            string notificationsListAsString = await notificationsResponse.Content.ReadAsStringAsync();
            usersWebNotifications = JsonConvert.DeserializeObject<List<WebNotification>>(notificationsListAsString);
            return usersWebNotifications;
        }

        /// <summary>
        /// Gets the number of WebNotifications for a given user, including both read and unread.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Integer with the number of WebNotifications.</returns>
        public async Task<int> GetUsersNotificationsCount(string userId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string apiPath = "/api/Notifications/GetUsersNotificationsCount/" + userId;
            HttpResponseMessage notificationsResponse = await _httpClient.GetAsync(apiPath);

            if (!notificationsResponse.IsSuccessStatusCode) return 0;

            string notificationsCountAsString = await notificationsResponse.Content.ReadAsStringAsync();
            int notificationsCount = JsonConvert.DeserializeObject<int>(notificationsCountAsString);
            return notificationsCount;

        }
    }
}
