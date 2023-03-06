using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class UserAccessHttpClient: IUserAccessHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public UserAccessHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
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

        public async Task<UserAccess?> AddUserAccess(UserAccess? userAccess)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/";
            HttpResponseMessage accessResponse = await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                userAccess = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
                return userAccess;
            }

            return new UserAccess();
        }

        public async Task<UserAccess?> UpdateUserAccess(UserAccess? userAccess)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateAccessApiPath = "/api/Access/" + userAccess?.AccessId;
            HttpResponseMessage accessResponse = await _httpClient.PutAsync(updateAccessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json"));
            if (accessResponse.IsSuccessStatusCode)
            {
                string userAccessAsString = await accessResponse.Content.ReadAsStringAsync();
                userAccess = JsonConvert.DeserializeObject<UserAccess>(userAccessAsString);
                return userAccess;
            }

            return new UserAccess();
        }

        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + userAccessId;
            HttpResponseMessage accessTokenResponse = await _httpClient.DeleteAsync(accessApiPath);
            if (accessTokenResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<UserAccess>?> GetProgenyAccessList(int progenyId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess>? accessList = new List<UserAccess>();
            string accessApiPath = "/api/Access/Progeny/" + progenyId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<List<UserAccess>?> GetUserAccessList(string userEmail)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            List<UserAccess>? accessList = new List<UserAccess>();
            string accessApiPath = "/api/Access/AccessListByUser/" + userEmail;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<UserAccess?> GetUserAccess(int userAccessId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string accessApiPath = "/api/Access/" + userAccessId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                UserAccess? accessItem = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
                return accessItem;
            }

            return new UserAccess();
        }
    }
}
