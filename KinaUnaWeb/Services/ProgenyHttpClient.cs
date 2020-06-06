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
using System.Net.Http.Formatting;

namespace KinaUnaWeb.Services
{
    public class ProgenyHttpClient:IProgenyHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public ProgenyHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string> GetNewToken()
        {
            //var accessToken = await _identityServerClient.RequestClientCredentialsTokenAsync();
            var access_token = await _apiTokenClient.GetApiToken(
                    _configuration.GetValue<string>("AuthenticationServerClientId"),
                    Constants.ProgenyApiName + " " + Constants.MediaApiName,
                    _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return access_token; // tokenResponse.AccessToken;
        }

        //public async Task<HttpClient> GetClient()
        //{
        //    string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
        //    var currentContext = _httpContextAccessor.HttpContext;

        //    string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
        //    if (!string.IsNullOrWhiteSpace(accessToken))
        //    {
        //        _httpClient.SetBearerToken(accessToken);
        //    }
        //    else
        //    {
        //        accessToken = await GetNewToken();
        //        _httpClient.SetBearerToken(accessToken);
        //    }

        //    _httpClient.BaseAddress = new Uri(clientUri);
        //    _httpClient.DefaultRequestHeaders.Accept.Clear();
        //    _httpClient.DefaultRequestHeaders.Accept.Add(
        //        new MediaTypeWithQualityHeaderValue("application/json"));

        //    return _httpClient;
        //}

        public async Task<UserInfo> GetUserInfo(string email)
        {
            // HttpClient httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;

            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //httpClient.BaseAddress = new Uri(clientUri);
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string userinfoApiPath = "api/userinfo/byemail/" + email;
            var userinfoResponse = await _httpClient.GetAsync(userinfoApiPath).ConfigureAwait(false);
            UserInfo userinfo = new UserInfo();
            if (userinfoResponse.IsSuccessStatusCode)
            {
                var userinfoAsString = await userinfoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                userinfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);
                
            }

            return userinfo;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string userId)
        {
            // HttpClient httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;

            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //httpClient.BaseAddress = new Uri(clientUri);
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string userinfoApiPath = "api/userinfo/byuserid/" + userId;
            var userinfoResponse = await _httpClient.GetAsync(userinfoApiPath).ConfigureAwait(false);
            UserInfo userinfo = new UserInfo();
            if (userinfoResponse.IsSuccessStatusCode)
            {
                var userinfoAsString = await userinfoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                userinfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);

            }

            return userinfo;
        }
        public async Task<UserInfo> UpdateUserInfo(UserInfo userinfo)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.UserId;
            //var newUserinfoUri = clientUri + newUserinfoApiPath;

            var newUserResponseString = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            var updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;
        }

        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }
            // HttpClient httpClient = new HttpClient();
            // string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
                
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //httpClient.BaseAddress = new Uri(clientUri);
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Progeny progeny = new Progeny();
            string progenyApiPath = "/api/progeny/" + progenyId;
            //var progenyUri = clientUri + progenyApiPath;
            

            try
            {
                var progenyResponse = await _httpClient.GetAsync(progenyApiPath).ConfigureAwait(false);

                if (progenyResponse.IsSuccessStatusCode)
                {
                    var progenyAsString = await progenyResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    progeny = JsonConvert.DeserializeObject<Progeny>(progenyAsString);
                }
                else
                {
                    progeny.Name = "401";
                    
                }
            }
            catch (Exception e)
            {
                progeny.Name = "401";
                progeny.NickName = e.Message;
                return progeny;
            }

            return progeny;
        }

        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string newProgenyApiPath = "/api/progeny/";
            //var newProgenyUri = clientUri + newProgenyApiPath;

            var newProgeny = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<Progeny>(newProgeny);
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateProgenyApiPath = "/api/progeny/" + progeny.Id;
            //var updateProgenyUri = clientUri + updateProgenyApiPath;
            
            var updateProgenyResponseString = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);
        }

        public async Task<bool> DeleteProgeny(int progenyId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string progenyApiPath = "/api/progeny/" + progenyId;
            //var progenyUri = clientUri + progenyApiPath;
            await _httpClient.DeleteAsync(progenyApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/adminlistbyuser/" + email;
            //var accessUri = clientUri + accessApiPath;
            var accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<Progeny> accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }

        public async Task<List<UserAccess>> GetProgenyAccessList(int progenyId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/progeny/" + progenyId;
            //var accessUri = clientUri + accessApiPath;
            var accessResponse = await _httpClient.GetAsync(accessApiPath).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                var accessAsString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> GetUserAccessList(string userEmail)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/accesslistbyuser/" + userEmail;
            //var accessUri = clientUri + accessApiPath;
            var accessResponse = await _httpClient.GetAsync(accessApiPath).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                var accessAsString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int userAccessId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            UserAccess accessItem = new UserAccess();
            string accessApiPath = "/api/access/" + userAccessId;
            //var accessUri = clientUri + accessApiPath;
            var accessResponse = await _httpClient.GetAsync(accessApiPath).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                var accessAsString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                accessItem = JsonConvert.DeserializeObject<UserAccess>(accessAsString);
            }

            return accessItem;
        }

        public async Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel)
        {
            List<Location> progenyLocations = new List<Location>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string locationsApiPath = "/api/locations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var locationsUri = clientUri + locationsApiPath;
            var locationsResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (locationsResponse.IsSuccessStatusCode)
            {
                var locationsAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyLocations = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            }

            return progenyLocations;
        }

        public async Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyPosts = new List<TimeLineItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string latestApiPath = "/api/timeline/progenylatest/" + progenyId + "/" + accessLevel + "/5/0";
            //var latestUri = clientUri + latestApiPath;
            var latestResponse = await _httpClient.GetAsync(latestApiPath).ConfigureAwait(false);
            if (latestResponse.IsSuccessStatusCode)
            {
                var latestAsString = await latestResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(latestAsString);
            }

            return progenyPosts;
        }

        public async Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel)
        {
            List<TimeLineItem> yearAgoPosts = new List<TimeLineItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string yearAgoApiPath = "/api/timeline/progenyyearago/" + progenyId + "/" + accessLevel;
            //var yearAgoUri = clientUri + yearAgoApiPath;
            var yearAgoResponse = await _httpClient.GetAsync(yearAgoApiPath).ConfigureAwait(false);
            if (yearAgoResponse.IsSuccessStatusCode)
            {
                var yearAgoAsString = await yearAgoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                yearAgoPosts = JsonConvert.DeserializeObject<List<TimeLineItem>>(yearAgoAsString);
            }

            return yearAgoPosts;
        }

        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/";
            //var accessUri = clientUri + accessApiPath;
            await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateAccessApiPath = "/api/access/" + userAccess.AccessId;
            //var updateAccessUri = clientUri + updateAccessApiPath;
            var updateAccessResponseString = await _httpClient.PutAsync(updateAccessApiPath, userAccess, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserAccess>(returnString);
        }

        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/" + userAccessId;
            //var accessUri = clientUri + accessApiPath;
            await _httpClient.DeleteAsync(accessApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<Sleep> GetSleepItem(int sleepId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Sleep sleepItem = new Sleep();
            string sleepApiPath = "/api/sleep/" + sleepId;
            //var sleepUri = clientUri + sleepApiPath;
            var sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                var sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            }

            return sleepItem;
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/";
            //var sleepUri = clientUri + sleepApiPath;
            await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return sleep;
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateSleepApiPath = "/api/sleep/" + sleep.SleepId;
            //var updateSleepUri = clientUri + updateSleepApiPath;
            var updateAccessResponseString = await _httpClient.PutAsync(updateSleepApiPath, sleep, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Sleep>(returnString);
        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/" + sleepId;
            //var sleepUri = clientUri + sleepApiPath;
            await _httpClient.DeleteAsync(sleepApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> progenySleepList = new List<Sleep>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var sleepUri = clientUri + sleepApiPath;
            var sleepResponse = await _httpClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                var sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenySleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepAsString);
            }

            return progenySleepList;
        }

        public async Task<CalendarItem> GetCalendarItem(int eventId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            CalendarItem calendarItem = new CalendarItem();
            string calendarApiPath = "/api/calendar/" + eventId;
            //var calendarUri = clientUri + calendarApiPath;
            var calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(calendarAsString);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> AddCalendarItem(CalendarItem eventItem)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/";
            //var calendarUri = clientUri + calendarApiPath;
            await _httpClient.PostAsync(calendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return eventItem;
        }

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateCalendarApiPath = "/api/calendar/" + eventItem.EventId;
            //var updateCalendarUri = clientUri + updateCalendarApiPath;
            var updateCalendarResponseString = await _httpClient.PutAsync(updateCalendarApiPath, eventItem, new JsonMediaTypeFormatter());
            string returnString = await updateCalendarResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CalendarItem>(returnString);
        }

        public async Task<bool> DeleteCalendarItem(int eventId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/" + eventId;
            //var calendarUri = clientUri + calendarApiPath;
            await _httpClient.DeleteAsync(calendarApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var calendarUri = clientUri + calendarApiPath;
            var calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);
            }

            return progenyCalendarList;
        }

        public async Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/eventlist/" + progenyId + "/" + accessLevel;
            //var calendarUri = clientUri + calendarApiPath;
            var calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);
            }

            return progenyCalendarList;
        }

        public async Task<Contact> GetContact(int contactId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Contact contactItem = new Contact();
            string contactsApiPath = "/api/contacts/" + contactId;
            //var contactsUri = clientUri + contactsApiPath;
            var contactResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (contactResponse.IsSuccessStatusCode)
            {
                var contactAsString = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                contactItem = JsonConvert.DeserializeObject<Contact>(contactAsString);
            }

            return contactItem;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string contactsApiPath = "/api/contacts/";
            //var contactsUri = clientUri + contactsApiPath;
            await _httpClient.PostAsync(contactsApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return contact;
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string updateContactApiPath = "/api/contacts/" + contact.ContactId;
            //var updateContactUri = clientUri + updateContactApiPath;
            var updateContactResponseString = await _httpClient.PutAsync(updateContactApiPath, contact, new JsonMediaTypeFormatter());
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        public async Task<bool> DeleteContact(int contactId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string contactApiPath = "/api/contacts/" + contactId;
            //var contactUri = clientUri + contactApiPath;
            await _httpClient.DeleteAsync(contactApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel)
        {
            List<Contact> progenyContactsList = new List<Contact>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string contactsApiPath = "/api/contacts/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var contactsUri = clientUri + contactsApiPath;
            var contactsResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (contactsResponse.IsSuccessStatusCode)
            {
                var contactsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyContactsList = JsonConvert.DeserializeObject<List<Contact>>(contactsAsString);
            }

            return progenyContactsList;
        }

        public async Task<Address> GetAddress(int addressId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Address addressItem = new Address();
            string addressApiPath = "/api/addresses/" + addressId;
            //var addressUri = clientUri + addressApiPath;
            var addressResponse = await _httpClient.GetAsync(addressApiPath).ConfigureAwait(false);
            if (addressResponse.IsSuccessStatusCode)
            {
                var addressAsString = await addressResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                addressItem = JsonConvert.DeserializeObject<Address>(addressAsString);
            }

            return addressItem;
        }

        public async Task<Address> AddAddress(Address address)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string addressApiPath = "/api/addresses/";
            //var addressUri = clientUri + addressApiPath;
            string returnString = await _httpClient.PostAsync(addressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Address>(returnString);
        }

        public async Task<Address> UpdateAddress(Address address)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateAddressApiPath = "/api/addresses/" + address.AddressId;
            //var updateAddressUri = clientUri + updateAddressApiPath;
            var updateAddressResponseString = await _httpClient.PutAsync(updateAddressApiPath, address, new JsonMediaTypeFormatter());
            string returnString = await updateAddressResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Address>(returnString);
        }

        public async Task<Friend> GetFriend(int friendId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Friend friendItem = new Friend();
            string friendsApiPath = "/api/friends/" + friendId;
            //var friendsUri = clientUri + friendsApiPath;
            var friendResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (friendResponse.IsSuccessStatusCode)
            {
                var friendAsString = await friendResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                friendItem = JsonConvert.DeserializeObject<Friend>(friendAsString);
            }

            return friendItem;
        }

        public async Task<Friend> AddFriend(Friend friend)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string friendsApiPath = "/api/friends/";
            //var friendsUri = clientUri + friendsApiPath;
            await _httpClient.PostAsync(friendsApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return friend;
        }

        public async Task<Friend> UpdateFriend(Friend friend)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateFriendApiPath = "/api/friends/" + friend.FriendId;
            //var updateFriendUri = clientUri + updateFriendApiPath;
            var updateFriendResponseString = await _httpClient.PutAsync(updateFriendApiPath, friend, new JsonMediaTypeFormatter());
            string returnString = await updateFriendResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Friend>(returnString);
        }

        public async Task<bool> DeleteFriend(int friendId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string friendsApiPath = "/api/friends/" + friendId;
            //var friendsUri = clientUri + friendsApiPath;
            await _httpClient.DeleteAsync(friendsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel)
        {
            List<Friend> progenyFriendsList = new List<Friend>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string friendsApiPath = "/api/friends/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var friendsUri = clientUri + friendsApiPath;
            var friendsResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (friendsResponse.IsSuccessStatusCode)
            {
                var friendsAsString = await friendsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyFriendsList = JsonConvert.DeserializeObject<List<Friend>>(friendsAsString);
            }

            return progenyFriendsList;
        }

        public async Task<Location> GetLocation(int locationId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Location locationItem = new Location();
            string locationsApiPath = "/api/locations/" + locationId;
            //var locationUri = clientUri + locationsApiPath;
            var locationResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (locationResponse.IsSuccessStatusCode)
            {
                var locationAsString = await locationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                locationItem = JsonConvert.DeserializeObject<Location>(locationAsString);
            }

            return locationItem;
        }

        public async Task<Location> AddLocation(Location location)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string locationsApiPath = "/api/locations/";
            //var locationUri = clientUri + locationsApiPath;
            await _httpClient.PostAsync(locationsApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return location;
        }

        public async Task<Location> UpdateLocation(Location location)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateApiPath = "/api/locations/" + location.LocationId;
            //var updateUri = clientUri + updateApiPath;
            var updateResponseString = await _httpClient.PutAsync(updateApiPath, location, new JsonMediaTypeFormatter());
            string returnString = await updateResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Location>(returnString);
        }

        public async Task<bool> DeleteLocation(int locationId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string locationsApiPath = "/api/locations/" + locationId;
            //var locationUri = clientUri + locationsApiPath;
            await _httpClient.DeleteAsync(locationsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Location>> GetLocationsList(int progenyId, int accessLevel)
        {
            List<Location> progenyLocationsList = new List<Location>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string locationsApiPath = "/api/locations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var locationsUri = clientUri + locationsApiPath;
            var locationsResponse = await _httpClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (locationsResponse.IsSuccessStatusCode)
            {
                var locationsAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyLocationsList = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            }

            return progenyLocationsList;
        }

        public async Task<Measurement> GetMeasurement(int measurementId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            Measurement measurementItem = new Measurement();
            string measurementsApiPath = "/api/measurements/" + measurementId;
            //var measurementUri = clientUri + measurementsApiPath;
            var measurementResponse = await _httpClient.GetAsync(measurementsApiPath).ConfigureAwait(false);
            if (measurementResponse.IsSuccessStatusCode)
            {
                var measurementAsString = await measurementResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                measurementItem = JsonConvert.DeserializeObject<Measurement>(measurementAsString);
            }

            return measurementItem;
        }

        public async Task<Measurement> AddMeasurement(Measurement measurement)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string measurementsApiPath = "/api/measurements/";
            //var measurementUri = clientUri + measurementsApiPath;
            await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return measurement;
        }

        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateMeasurementsApiPath = "/api/measurements/" + measurement.MeasurementId;
            //var updateMeasurementUri = clientUri + updateMeasurementsApiPath;
            var updateMeasurementResponseString = await _httpClient.PutAsync(updateMeasurementsApiPath, measurement, new JsonMediaTypeFormatter());
            string returnString = await updateMeasurementResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Measurement>(returnString);
        }

        public async Task<bool> DeleteMeasurement(int measurementId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string measurementsApiPath = "/api/measurements/" + measurementId;
            //var measurementUri = clientUri + measurementsApiPath;
            await _httpClient.DeleteAsync(measurementsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel)
        {
            List<Measurement> progenyMeasurementsList = new List<Measurement>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string measurementsApiPath = "/api/measurements/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var measurementsUri = clientUri + measurementsApiPath;
            var measurementsResponse = await _httpClient.GetAsync(measurementsApiPath).ConfigureAwait(false);
            if (measurementsResponse.IsSuccessStatusCode)
            {
                var measurementsAsString = await measurementsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyMeasurementsList = JsonConvert.DeserializeObject<List<Measurement>>(measurementsAsString);
            }

            return progenyMeasurementsList;
        }

        public async Task<Note> GetNote(int noteId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Note noteItem = new Note();
            string notesApiPath = "/api/notes/" + noteId;
            //var noteUri = clientUri + notesApiPath;
            var noteResponse = await _httpClient.GetAsync(notesApiPath).ConfigureAwait(false);
            if (noteResponse.IsSuccessStatusCode)
            {
                var noteAsString = await noteResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                noteItem = JsonConvert.DeserializeObject<Note>(noteAsString);
            }

            return noteItem;
        }

        public async Task<Note> AddNote(Note note)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string notesApiPath = "/api/notes/";
            //var noteUri = clientUri + notesApiPath;
            await _httpClient.PostAsync(notesApiPath, new StringContent(JsonConvert.SerializeObject(note), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return note;
        }

        public async Task<Note> UpdateNote(Note note)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string updateApiPath = "/api/notes/" + note.NoteId;
            //var updateUri = clientUri + updateApiPath;
            var updateResponseString = await _httpClient.PutAsync(updateApiPath, note, new JsonMediaTypeFormatter());
            string returnString = await updateResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Note>(returnString);
        }

        public async Task<bool> DeleteNote(int noteId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string notesApiPath = "/api/notes/" + noteId;
            //var noteUri = clientUri + notesApiPath;
            await _httpClient.DeleteAsync(notesApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Note>> GetNotesList(int progenyId, int accessLevel)
        {
            List<Note> progenyNotesList = new List<Note>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string notesApiPath = "/api/notes/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var notesUri = clientUri + notesApiPath;
            var notesResponse = await _httpClient.GetAsync(notesApiPath).ConfigureAwait(false);
            if (notesResponse.IsSuccessStatusCode)
            {
                var notesAsString = await notesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyNotesList = JsonConvert.DeserializeObject<List<Note>>(notesAsString);
            }

            return progenyNotesList;
        }

        public async Task<Skill> GetSkill(int skillId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            Skill skillItem = new Skill();
            string skillsApiPath = "/api/skills/" + skillId;
            //var skillUri = clientUri + skillsApiPath;
            var skillResponse = await _httpClient.GetAsync(skillsApiPath).ConfigureAwait(false);
            if (skillResponse.IsSuccessStatusCode)
            {
                var skillAsString = await skillResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                skillItem = JsonConvert.DeserializeObject<Skill>(skillAsString);
            }

            return skillItem;
        }

        public async Task<Skill> AddSkill(Skill skill)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string skillsApiPath = "/api/skills/";
            //var skillUri = clientUri + skillsApiPath;
            await _httpClient.PostAsync(skillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return skill;
        }

        public async Task<Skill> UpdateSkill(Skill skill)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateSkillsApiPath = "/api/skills/" + skill.SkillId;
            //var updateSkillUri = clientUri + updateSkillsApiPath;
            var updateSkillResponseString = await _httpClient.PutAsync(updateSkillsApiPath, skill, new JsonMediaTypeFormatter());
            string returnString = await updateSkillResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Skill>(returnString);
        }

        public async Task<bool> DeleteSkill(int skillId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string skillsApiPath = "/api/skills/" + skillId;
            //var skillUri = clientUri + skillsApiPath;
            await _httpClient.DeleteAsync(skillsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId, int accessLevel)
        {
            List<Skill> progenySkillsList = new List<Skill>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string skillsApiPath = "/api/skills/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var skillsUri = clientUri + skillsApiPath;
            var skillsResponse = await _httpClient.GetAsync(skillsApiPath).ConfigureAwait(false);
            if (skillsResponse.IsSuccessStatusCode)
            {
                var skillsAsString = await skillsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenySkillsList = JsonConvert.DeserializeObject<List<Skill>>(skillsAsString);
            }

            return progenySkillsList;
        }

        public async Task<Vaccination> GetVaccination(int vaccinationId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            Vaccination vaccinationItem = new Vaccination();
            string vaccinationsApiPath = "/api/vaccinations/" + vaccinationId;
            //var vaccinationUri = clientUri + vaccinationsApiPath;
            var vaccinationResponse = await _httpClient.GetAsync(vaccinationsApiPath).ConfigureAwait(false);
            if (vaccinationResponse.IsSuccessStatusCode)
            {
                var vaccinationAsString = await vaccinationResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                vaccinationItem = JsonConvert.DeserializeObject<Vaccination>(vaccinationAsString);
            }

            return vaccinationItem;
        }

        public async Task<Vaccination> AddVaccination(Vaccination vaccination)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string vaccinationsApiPath = "/api/vaccinations/";
            //var vaccinationUri = clientUri + vaccinationsApiPath;
            await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return vaccination;
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string updateVaccinationsApiPath = "/api/vaccinations/" + vaccination.VaccinationId;
            //var updateVaccinationUri = clientUri + updateVaccinationsApiPath;
            var updateVaccinationResponseString = await _httpClient.PutAsync(updateVaccinationsApiPath, vaccination, new JsonMediaTypeFormatter());
            string returnString = await updateVaccinationResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Vaccination>(returnString);
        }

        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string vaccinationsApiPath = "/api/vaccinations/" + vaccinationId;
            //var vaccinationUri = clientUri + vaccinationsApiPath;
            await _httpClient.DeleteAsync(vaccinationsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel)
        {
            List<Vaccination> progenyVaccinationsList = new List<Vaccination>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string vaccinationsApiPath = "/api/vaccinations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var vaccinationsUri = clientUri + vaccinationsApiPath;
            var vaccinationsResponse = await _httpClient.GetAsync(vaccinationsApiPath).ConfigureAwait(false);
            if (vaccinationsResponse.IsSuccessStatusCode)
            {
                var vaccinationsAsString = await vaccinationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyVaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(vaccinationsAsString);
            }

            return progenyVaccinationsList;
        }

        public async Task<VocabularyItem> GetWord(int wordId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            VocabularyItem wordItem = new VocabularyItem();
            string vocabularyApiPath = "/api/vocabulary/" + wordId;
            //var wordUri = clientUri + vocabularyApiPath;
            var wordResponse = await _httpClient.GetAsync(vocabularyApiPath).ConfigureAwait(false);
            if (wordResponse.IsSuccessStatusCode)
            {
                var wordAsString = await wordResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                wordItem = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
            }

            return wordItem;
        }

        public async Task<VocabularyItem> AddWord(VocabularyItem word)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string vocabularyApiPath = "/api/vocabulary/";
            //var wordUri = clientUri + vocabularyApiPath;
            await _httpClient.PostAsync(vocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return word;
        }

        public async Task<VocabularyItem> UpdateWord(VocabularyItem word)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateVocabularyApiPath = "/api/vocabulary/" + word.WordId;
            //var updateWordUri = clientUri + updateVocabularyApiPath;
            var updateWordResponseString = await _httpClient.PutAsync(updateVocabularyApiPath, word, new JsonMediaTypeFormatter());
            string returnString = await updateWordResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<VocabularyItem>(returnString);
        }

        public async Task<bool> DeleteWord(int wordId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));

            string vocabularyApiPath = "/api/vocabulary/" + wordId;
            //var wordUri = clientUri + vocabularyApiPath;
            await _httpClient.DeleteAsync(vocabularyApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<VocabularyItem>> GetWordsList(int progenyId, int accessLevel)
        {
            List<VocabularyItem> progenyWordsList = new List<VocabularyItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
              //  new MediaTypeWithQualityHeaderValue("application/json"));

            string vocabularyApiPath = "/api/vocabulary/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var wordsUri = clientUri + vocabularyApiPath;
            var wordsResponse = await _httpClient.GetAsync(vocabularyApiPath).ConfigureAwait(false);
            if (wordsResponse.IsSuccessStatusCode)
            {
                var wordsAsString = await wordsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyWordsList = JsonConvert.DeserializeObject<List<VocabularyItem>>(wordsAsString);
            }

            return progenyWordsList;
        }

        public async Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            TimeLineItem timeLineItem = new TimeLineItem();
            string timeLineApiPath = "/api/timeline/" + "gettimelineitembyitemid/" + itemId + "/" + itemType;
            //var timeLineUri = clientUri + timeLineApiPath;
            var timeLineResponse = await _httpClient.GetAsync(timeLineApiPath).ConfigureAwait(false);
            if (timeLineResponse.IsSuccessStatusCode)
            {
                var timeLineItemAsString = await timeLineResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            }

            return timeLineItem;
        }

        public async Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string timeLineApiPath = "/api/timeline/";
            //var timeLineUri = clientUri + timeLineApiPath;
            await _httpClient.PostAsync(timeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return timeLineItem;
        }

        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string updateTimeLineApiPath = "/api/timeline/" + timeLineItem.TimeLineId;
            //var updateTimeLineUri = clientUri + updateTimeLineApiPath;
            var updateTimeLineResponseString = await _httpClient.PutAsync(updateTimeLineApiPath, timeLineItem, new JsonMediaTypeFormatter());
            string returnString = await updateTimeLineResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TimeLineItem>(returnString);
        }

        public async Task<bool> DeleteTimeLineItem(int timeLineItemId)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string timeLineApiPath = "/api/timeline/" + timeLineItemId;
            //var timeLineUri = clientUri + timeLineApiPath;
            await _httpClient.DeleteAsync(timeLineApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyTimeline = new List<TimeLineItem>();
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }

            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string timelineApiPath = "/api/timeline/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            //var timelineUri = clientUri + timelineApiPath;
            var timelineResponse = await _httpClient.GetAsync(timelineApiPath).ConfigureAwait(false);
            if (timelineResponse.IsSuccessStatusCode)
            {
                var timelineAsString = await timelineResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyTimeline = JsonConvert.DeserializeObject<List<TimeLineItem>>(timelineAsString);
            }

            return progenyTimeline;
        }

        public async Task SetViewChild(string userId, UserInfo userinfo)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            

            string setChildApiPath = "/api/userinfo/" + userId;
            //var setChildUri = clientUri + setChildApiPath;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }
    }
}
