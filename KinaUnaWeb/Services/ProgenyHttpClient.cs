using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public class ProgenyHttpClient:IProgenyHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ProgenyHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;

        }

        public async Task<string> GetNewToken()
        {
            var discoveryClient = new HttpClient();

            var tokenResponse = await discoveryClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = _configuration.GetValue<string>("AuthenticationServer") + "/connect/token",

                ClientId = _configuration.GetValue<string>("AuthenticationServerClientId"),
                ClientSecret = _configuration.GetValue<string>("AuthenticationServerClientSecret"),
                Scope = Constants.ProgenyApiName
            });

            return tokenResponse.AccessToken;
        }
        public async Task<HttpClient> GetClient()
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
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

            _httpClient.BaseAddress = new Uri(clientUri);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }

        public async Task<UserInfo> GetUserInfo(string email)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;

            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string userinfoApiPath = "api/userinfo/byemail/" + email;
            var userinfoResponse = await httpClient.GetAsync(userinfoApiPath).ConfigureAwait(false);
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
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;

            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string userinfoApiPath = "api/userinfo/byuserid/" + userId;
            var userinfoResponse = await httpClient.GetAsync(userinfoApiPath).ConfigureAwait(false);
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
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient newUserinfoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newUserinfoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newUserinfoHttpClient.SetBearerToken(accessToken);
            }
            newUserinfoHttpClient.BaseAddress = new Uri(clientUri);
            newUserinfoHttpClient.DefaultRequestHeaders.Accept.Clear();
            newUserinfoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.UserId;
            var newUserinfoUri = clientUri + newUserinfoApiPath;

            var newUserResponseString = await newUserinfoHttpClient.PutAsync(newUserinfoUri, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            var updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;
        }

        public async Task<Progeny> GetProgeny(int progenyId)
        {
            if (progenyId == 0)
            {
                progenyId = Constants.DefaultChildId;
            }
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
                
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Progeny progeny = new Progeny();
            string progenyApiPath = "/api/progeny/" + progenyId;
            var progenyUri = clientUri + progenyApiPath;
            

            try
            {
                var progenyResponseString = await httpClient.GetStringAsync(progenyUri);
                progeny = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);
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
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient newProgenyHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newProgenyHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newProgenyHttpClient.SetBearerToken(accessToken);
            }
            newProgenyHttpClient.BaseAddress = new Uri(clientUri);
            newProgenyHttpClient.DefaultRequestHeaders.Accept.Clear();
            newProgenyHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string newProgenyApiPath = "/api/progeny/";
            var newProgenyUri = clientUri + newProgenyApiPath;

            var newProgeny = await newProgenyHttpClient.PostAsync(newProgenyUri, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<Progeny>(newProgeny);
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateProgenyHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateProgenyHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateProgenyHttpClient.SetBearerToken(accessToken);
            }
            updateProgenyHttpClient.BaseAddress = new Uri(clientUri);
            updateProgenyHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateProgenyHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateProgenyApiPath = "/api/progeny/" + progeny.Id;
            var updateProgenyUri = clientUri + updateProgenyApiPath;
            
            var updateProgenyResponseString = await updateProgenyHttpClient.PutAsync(updateProgenyUri, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);
        }

        public async Task<bool> DeleteProgeny(int progenyId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string progenyApiPath = "/api/progeny/" + progenyId;
            var progenyUri = clientUri + progenyApiPath;
            await httpClient.DeleteAsync(progenyUri).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/adminlistbyuser/" + email;
            var accessUri = clientUri + accessApiPath;
            var accessResponseString = await httpClient.GetStringAsync(accessUri);
            List<Progeny> accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }

        public async Task<List<UserAccess>> GetProgenyAccessList(int progenyId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/progeny/" + progenyId;
            var accessUri = clientUri + accessApiPath;
            var accessResponse = await httpClient.GetAsync(accessUri).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                var accessAsString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> GetUserAccessList(string userEmail)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/accesslistbyuser/" + userEmail;
            var accessUri = clientUri + accessApiPath;
            var accessResponse = await httpClient.GetAsync(accessUri).ConfigureAwait(false);
            if (accessResponse.IsSuccessStatusCode)
            {
                var accessAsString = await accessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);
            }

            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int userAccessId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            UserAccess accessItem = new UserAccess();
            string accessApiPath = "/api/access/" + userAccessId;
            var accessUri = clientUri + accessApiPath;
            var accessResponse = await httpClient.GetAsync(accessUri).ConfigureAwait(false);
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
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string locationsApiPath = "/api/locations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            var locationsUri = clientUri + locationsApiPath;
            var locationsResponse = await httpClient.GetAsync(locationsUri).ConfigureAwait(false);
            if (locationsResponse.IsSuccessStatusCode)
            {
                var locationsAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyLocations = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            }

            return progenyLocations;
        }

        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/";
            var accessUri = clientUri + accessApiPath;
            await httpClient.PostAsync(accessUri, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateAccessHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateAccessHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateAccessHttpClient.SetBearerToken(accessToken);
            }
            updateAccessHttpClient.BaseAddress = new Uri(clientUri);
            updateAccessHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateAccessHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateAccessApiPath = "/api/access/" + userAccess.AccessId;
            var updateAccessUri = clientUri + updateAccessApiPath;
            var updateAccessResponseString = await updateAccessHttpClient.PutAsync(updateAccessUri, userAccess, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserAccess>(returnString);
        }

        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/" + userAccessId;
            var accessUri = clientUri + accessApiPath;
            await httpClient.DeleteAsync(accessUri).ConfigureAwait(false);

            return true;
        }

        public async Task<Sleep> GetSleepItem(int sleepId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Sleep sleepItem = new Sleep();
            string sleepApiPath = "/api/sleep/" + sleepId;
            var sleepUri = clientUri + sleepApiPath;
            var sleepResponse = await httpClient.GetAsync(sleepUri).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                var sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                sleepItem = JsonConvert.DeserializeObject<Sleep>(sleepAsString);
            }

            return sleepItem;
        }

        public async Task<Sleep> AddSleep(Sleep sleep)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/";
            var sleepUri = clientUri + sleepApiPath;
            await httpClient.PostAsync(sleepUri, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return sleep;
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateSleepHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateSleepHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateSleepHttpClient.SetBearerToken(accessToken);
            }
            updateSleepHttpClient.BaseAddress = new Uri(clientUri);
            updateSleepHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateSleepHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateSleepApiPath = "/api/sleep/" + sleep.SleepId;
            var updateSleepUri = clientUri + updateSleepApiPath;
            var updateAccessResponseString = await updateSleepHttpClient.PutAsync(updateSleepUri, sleep, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Sleep>(returnString);
        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/" + sleepId;
            var sleepUri = clientUri + sleepApiPath;
            await httpClient.DeleteAsync(sleepUri).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> progenySleepList = new List<Sleep>();
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/sleep/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            var sleepUri = clientUri + sleepApiPath;
            var sleepResponse = await httpClient.GetAsync(sleepUri).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                var sleepAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenySleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepAsString);
            }

            return progenySleepList;
        }

        public async Task<CalendarItem> GetCalendarItem(int eventId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            CalendarItem calendarItem = new CalendarItem();
            string calendarApiPath = "/api/calendar/" + eventId;
            var calendarUri = clientUri + calendarApiPath;
            var calendarResponse = await httpClient.GetAsync(calendarUri).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(calendarAsString);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> AddCalendarItem(CalendarItem eventItem)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/";
            var calendarUri = clientUri + calendarApiPath;
            await httpClient.PostAsync(calendarUri, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return eventItem;
        }

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateCalendarHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateCalendarHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateCalendarHttpClient.SetBearerToken(accessToken);
            }
            updateCalendarHttpClient.BaseAddress = new Uri(clientUri);
            updateCalendarHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateCalendarHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateCalendarApiPath = "/api/calendar/" + eventItem.EventId;
            var updateCalendarUri = clientUri + updateCalendarApiPath;
            var updateCalendarResponseString = await updateCalendarHttpClient.PutAsync(updateCalendarUri, eventItem, new JsonMediaTypeFormatter());
            string returnString = await updateCalendarResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CalendarItem>(returnString);
        }

        public async Task<bool> DeleteCalendarItem(int eventId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/" + eventId;
            var calendarUri = clientUri + calendarApiPath;
            await httpClient.DeleteAsync(calendarUri).ConfigureAwait(false);

            return true;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string calendarApiPath = "/api/calendar/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            var calendarUri = clientUri + calendarApiPath;
            var calendarResponse = await httpClient.GetAsync(calendarUri).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);
            }

            return progenyCalendarList;
        }

        public async Task<Contact> GetContact(int contactId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Contact contactItem = new Contact();
            string contactsApiPath = "/api/contacts/" + contactId;
            var contactsUri = clientUri + contactsApiPath;
            var contactResponse = await httpClient.GetAsync(contactsUri).ConfigureAwait(false);
            if (contactResponse.IsSuccessStatusCode)
            {
                var contactAsString = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                contactItem = JsonConvert.DeserializeObject<Contact>(contactAsString);
            }

            return contactItem;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string contactsApiPath = "/api/contacts/";
            var contactsUri = clientUri + contactsApiPath;
            await httpClient.PostAsync(contactsUri, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return contact;
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateHttpClient.SetBearerToken(accessToken);
            }
            updateHttpClient.BaseAddress = new Uri(clientUri);
            updateHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateContactApiPath = "/api/contacts/" + contact.ContactId;
            var updateContactUri = clientUri + updateContactApiPath;
            var updateContactResponseString = await updateHttpClient.PutAsync(updateContactUri, contact, new JsonMediaTypeFormatter());
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        public async Task<bool> DeleteContact(int contactId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string contactApiPath = "/api/contacts/" + contactId;
            var contactUri = clientUri + contactApiPath;
            await httpClient.DeleteAsync(contactUri).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel)
        {
            List<Contact> progenyContactsList = new List<Contact>();
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string contactsApiPath = "/api/contacts/progeny/" + progenyId + "?accessLevel=" + accessLevel;
            var contactsUri = clientUri + contactsApiPath;
            var contactsResponse = await httpClient.GetAsync(contactsUri).ConfigureAwait(false);
            if (contactsResponse.IsSuccessStatusCode)
            {
                var contactsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyContactsList = JsonConvert.DeserializeObject<List<Contact>>(contactsAsString);
            }

            return progenyContactsList;
        }

        public async Task<Address> GetAddress(int addressId)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            Address addressItem = new Address();
            string addressApiPath = "/api/addresses/" + addressId;
            var addressUri = clientUri + addressApiPath;
            var addressResponse = await httpClient.GetAsync(addressUri).ConfigureAwait(false);
            if (addressResponse.IsSuccessStatusCode)
            {
                var addressAsString = await addressResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                addressItem = JsonConvert.DeserializeObject<Address>(addressAsString);
            }

            return addressItem;
        }

        public async Task<Address> AddAddress(Address address)
        {
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string addressApiPath = "/api/addresses/";
            var addressUri = clientUri + addressApiPath;
            string returnString = await httpClient.PostAsync(addressUri, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Address>(returnString);
        }

        public async Task<Address> UpdateAddress(Address address)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateHttpClient.SetBearerToken(accessToken);
            }
            updateHttpClient.BaseAddress = new Uri(clientUri);
            updateHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateAddressApiPath = "/api/addresses/" + address.AddressId;
            var updateAddressUri = clientUri + updateAddressApiPath;
            var updateAddressResponseString = await updateHttpClient.PutAsync(updateAddressUri, address, new JsonMediaTypeFormatter());
            string returnString = await updateAddressResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Address>(returnString);
        }
    }
}
