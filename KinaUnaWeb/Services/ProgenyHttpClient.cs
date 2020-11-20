using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using System.Net.Http.Formatting;
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
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
           
        }

        private async Task<string> GetNewToken()
        {
            var authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
            }

            var access_token = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return access_token;
        }

        public async Task<UserInfo> GetUserInfo(string email)
        {
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
            
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.UserId;
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

            Progeny progeny = new Progeny();
            string progenyApiPath = "/api/progeny/" + progenyId;

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

            string newProgenyApiPath = "/api/progeny/";
            var newProgeny = await _httpClient.PostAsync(newProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<Progeny>(newProgeny);
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
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

            string updateProgenyApiPath = "/api/progeny/" + progeny.Id;
            
            var updateProgenyResponseString = await _httpClient.PutAsync(updateProgenyApiPath, new StringContent(JsonConvert.SerializeObject(progeny), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Progeny>(updateProgenyResponseString);
        }

        public async Task<bool> DeleteProgeny(int progenyId)
        {
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

            string progenyApiPath = "/api/progeny/" + progenyId;
            await _httpClient.DeleteAsync(progenyApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Progeny>> GetProgenyAdminList(string email)
        {
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

            string accessApiPath = "/api/access/adminlistbyuser/" + email;
            var accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<Progeny> accessList = JsonConvert.DeserializeObject<List<Progeny>>(accessResponseString);

            return accessList;
        }

        public async Task<List<UserAccess>> GetProgenyAccessList(int progenyId)
        {
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

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/progeny/" + progenyId;
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

            List<UserAccess> accessList = new List<UserAccess>();
            string accessApiPath = "/api/access/accesslistbyuser/" + userEmail;
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

            
            UserAccess accessItem = new UserAccess();
            string accessApiPath = "/api/access/" + userAccessId;
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

            string locationsApiPath = "/api/locations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            string latestApiPath = "/api/timeline/progenylatest/" + progenyId + "/" + accessLevel + "/5/0";
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

            string yearAgoApiPath = "/api/timeline/progenyyearago/" + progenyId + "/" + accessLevel;
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

            string accessApiPath = "/api/access/";
            await _httpClient.PostAsync(accessApiPath, new StringContent(JsonConvert.SerializeObject(userAccess), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
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

            string updateAccessApiPath = "/api/access/" + userAccess.AccessId;
            var updateAccessResponseString = await _httpClient.PutAsync(updateAccessApiPath, userAccess, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserAccess>(returnString);
        }

        public async Task<bool> DeleteUserAccess(int userAccessId)
        {
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

            string accessApiPath = "/api/access/" + userAccessId;

            await _httpClient.DeleteAsync(accessApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<Sleep> GetSleepItem(int sleepId)
        {
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

            Sleep sleepItem = new Sleep();
            string sleepApiPath = "/api/sleep/" + sleepId;
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

            string sleepApiPath = "/api/sleep/";
            await _httpClient.PostAsync(sleepApiPath, new StringContent(JsonConvert.SerializeObject(sleep), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return sleep;
        }

        public async Task<Sleep> UpdateSleep(Sleep sleep)
        {
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

            string updateSleepApiPath = "/api/sleep/" + sleep.SleepId;
            var updateAccessResponseString = await _httpClient.PutAsync(updateSleepApiPath, sleep, new JsonMediaTypeFormatter());
            string returnString = await updateAccessResponseString.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Sleep>(returnString);
        }

        public async Task<bool> DeleteSleepItem(int sleepId)
        {
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

            string sleepApiPath = "/api/sleep/" + sleepId;
            await _httpClient.DeleteAsync(sleepApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel)
        {
            List<Sleep> progenySleepList = new List<Sleep>();
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

            string sleepApiPath = "/api/sleep/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            CalendarItem calendarItem = new CalendarItem();
            string calendarApiPath = "/api/calendar/" + eventId;
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

            string calendarApiPath = "/api/calendar/";
            await _httpClient.PostAsync(calendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return eventItem;
        }

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem)
        {
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

            string updateCalendarApiPath = "/api/calendar/" + eventItem.EventId;
            var updateCalendarResponseString = await _httpClient.PutAsync(updateCalendarApiPath, eventItem, new JsonMediaTypeFormatter());
            string returnString = await updateCalendarResponseString.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarItem>(returnString);
        }

        public async Task<bool> DeleteCalendarItem(int eventId)
        {
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

            string calendarApiPath = "/api/calendar/" + eventId;
            await _httpClient.DeleteAsync(calendarApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
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

            string calendarApiPath = "/api/calendar/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            string calendarApiPath = "/api/calendar/eventlist/" + progenyId + "/" + accessLevel;
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

            Contact contactItem = new Contact();
            string contactsApiPath = "/api/contacts/" + contactId;
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

            string contactsApiPath = "/api/contacts/";
            await _httpClient.PostAsync(contactsApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return contact;
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
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
            
            string updateContactApiPath = "/api/contacts/" + contact.ContactId;
            var updateContactResponseString = await _httpClient.PutAsync(updateContactApiPath, contact, new JsonMediaTypeFormatter());
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        public async Task<bool> DeleteContact(int contactId)
        {
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

            string contactApiPath = "/api/contacts/" + contactId;
            await _httpClient.DeleteAsync(contactApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel)
        {
            List<Contact> progenyContactsList = new List<Contact>();
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

            string contactsApiPath = "/api/contacts/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Address addressItem = new Address();
            string addressApiPath = "/api/addresses/" + addressId;
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

            string addressApiPath = "/api/addresses/";
            string returnString = await _httpClient.PostAsync(addressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Address>(returnString);
        }

        public async Task<Address> UpdateAddress(Address address)
        {
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

            string updateAddressApiPath = "/api/addresses/" + address.AddressId;
            var updateAddressResponseString = await _httpClient.PutAsync(updateAddressApiPath, address, new JsonMediaTypeFormatter());
            string returnString = await updateAddressResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Address>(returnString);
        }

        public async Task<Friend> GetFriend(int friendId)
        {
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

            Friend friendItem = new Friend();
            string friendsApiPath = "/api/friends/" + friendId;
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

            string friendsApiPath = "/api/friends/";
            await _httpClient.PostAsync(friendsApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return friend;
        }

        public async Task<Friend> UpdateFriend(Friend friend)
        {
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
            
            string updateFriendApiPath = "/api/friends/" + friend.FriendId;
            var updateFriendResponseString = await _httpClient.PutAsync(updateFriendApiPath, friend, new JsonMediaTypeFormatter());
            string returnString = await updateFriendResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Friend>(returnString);
        }

        public async Task<bool> DeleteFriend(int friendId)
        {
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
                        
            string friendsApiPath = "/api/friends/" + friendId;
            await _httpClient.DeleteAsync(friendsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel)
        {
            List<Friend> progenyFriendsList = new List<Friend>();
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

            string friendsApiPath = "/api/friends/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Location locationItem = new Location();
            string locationsApiPath = "/api/locations/" + locationId;
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

            string locationsApiPath = "/api/locations/";
            await _httpClient.PostAsync(locationsApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return location;
        }

        public async Task<Location> UpdateLocation(Location location)
        {
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
            
            string updateApiPath = "/api/locations/" + location.LocationId;
            var updateResponseString = await _httpClient.PutAsync(updateApiPath, location, new JsonMediaTypeFormatter());
            string returnString = await updateResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Location>(returnString);
        }

        public async Task<bool> DeleteLocation(int locationId)
        {
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

            string locationsApiPath = "/api/locations/" + locationId;
            await _httpClient.DeleteAsync(locationsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Location>> GetLocationsList(int progenyId, int accessLevel)
        {
            List<Location> progenyLocationsList = new List<Location>();
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

            string locationsApiPath = "/api/locations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Measurement measurementItem = new Measurement();
            string measurementsApiPath = "/api/measurements/" + measurementId;
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

            string measurementsApiPath = "/api/measurements/";
            await _httpClient.PostAsync(measurementsApiPath, new StringContent(JsonConvert.SerializeObject(measurement), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return measurement;
        }

        public async Task<Measurement> UpdateMeasurement(Measurement measurement)
        {
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

            string updateMeasurementsApiPath = "/api/measurements/" + measurement.MeasurementId;
            var updateMeasurementResponseString = await _httpClient.PutAsync(updateMeasurementsApiPath, measurement, new JsonMediaTypeFormatter());
            string returnString = await updateMeasurementResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Measurement>(returnString);
        }

        public async Task<bool> DeleteMeasurement(int measurementId)
        {
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

            string measurementsApiPath = "/api/measurements/" + measurementId;
            await _httpClient.DeleteAsync(measurementsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel)
        {
            List<Measurement> progenyMeasurementsList = new List<Measurement>();
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

            string measurementsApiPath = "/api/measurements/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Note noteItem = new Note();
            string notesApiPath = "/api/notes/" + noteId;
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

            string notesApiPath = "/api/notes/";
            await _httpClient.PostAsync(notesApiPath, new StringContent(JsonConvert.SerializeObject(note), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return note;
        }

        public async Task<Note> UpdateNote(Note note)
        {
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

            string updateApiPath = "/api/notes/" + note.NoteId;
            var updateResponseString = await _httpClient.PutAsync(updateApiPath, note, new JsonMediaTypeFormatter());
            string returnString = await updateResponseString.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Note>(returnString);
        }

        public async Task<bool> DeleteNote(int noteId)
        {
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

            string notesApiPath = "/api/notes/" + noteId;
            await _httpClient.DeleteAsync(notesApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Note>> GetNotesList(int progenyId, int accessLevel)
        {
            List<Note> progenyNotesList = new List<Note>();
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

            string notesApiPath = "/api/notes/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Skill skillItem = new Skill();
            string skillsApiPath = "/api/skills/" + skillId;
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

            string skillsApiPath = "/api/skills/";
            await _httpClient.PostAsync(skillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return skill;
        }

        public async Task<Skill> UpdateSkill(Skill skill)
        {
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

            string updateSkillsApiPath = "/api/skills/" + skill.SkillId;
            var updateSkillResponseString = await _httpClient.PutAsync(updateSkillsApiPath, skill, new JsonMediaTypeFormatter());
            string returnString = await updateSkillResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Skill>(returnString);
        }

        public async Task<bool> DeleteSkill(int skillId)
        {
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

            string skillsApiPath = "/api/skills/" + skillId;
            await _httpClient.DeleteAsync(skillsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId, int accessLevel)
        {
            List<Skill> progenySkillsList = new List<Skill>();
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

            string skillsApiPath = "/api/skills/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            Vaccination vaccinationItem = new Vaccination();
            string vaccinationsApiPath = "/api/vaccinations/" + vaccinationId;
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

            string vaccinationsApiPath = "/api/vaccinations/";
            await _httpClient.PostAsync(vaccinationsApiPath, new StringContent(JsonConvert.SerializeObject(vaccination), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return vaccination;
        }

        public async Task<Vaccination> UpdateVaccination(Vaccination vaccination)
        {
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

            string updateVaccinationsApiPath = "/api/vaccinations/" + vaccination.VaccinationId;
            var updateVaccinationResponseString = await _httpClient.PutAsync(updateVaccinationsApiPath, vaccination, new JsonMediaTypeFormatter());
            string returnString = await updateVaccinationResponseString.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Vaccination>(returnString);
        }

        public async Task<bool> DeleteVaccination(int vaccinationId)
        {
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

            string vaccinationsApiPath = "/api/vaccinations/" + vaccinationId;
            await _httpClient.DeleteAsync(vaccinationsApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel)
        {
            List<Vaccination> progenyVaccinationsList = new List<Vaccination>();
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

            string vaccinationsApiPath = "/api/vaccinations/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            VocabularyItem wordItem = new VocabularyItem();
            string vocabularyApiPath = "/api/vocabulary/" + wordId;
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

            string vocabularyApiPath = "/api/vocabulary/";
            await _httpClient.PostAsync(vocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return word;
        }

        public async Task<VocabularyItem> UpdateWord(VocabularyItem word)
        {
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
            
            string updateVocabularyApiPath = "/api/vocabulary/" + word.WordId;
            var updateWordResponseString = await _httpClient.PutAsync(updateVocabularyApiPath, word, new JsonMediaTypeFormatter());
            string returnString = await updateWordResponseString.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<VocabularyItem>(returnString);
        }

        public async Task<bool> DeleteWord(int wordId)
        {
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

            string vocabularyApiPath = "/api/vocabulary/" + wordId;
            await _httpClient.DeleteAsync(vocabularyApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<VocabularyItem>> GetWordsList(int progenyId, int accessLevel)
        {
            List<VocabularyItem> progenyWordsList = new List<VocabularyItem>();
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

            string vocabularyApiPath = "/api/vocabulary/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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

            TimeLineItem timeLineItem = new TimeLineItem();
            string timeLineApiPath = "/api/timeline/" + "gettimelineitembyitemid/" + itemId + "/" + itemType;
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

            string timeLineApiPath = "/api/timeline/";
            await _httpClient.PostAsync(timeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();

            return timeLineItem;
        }

        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem)
        {
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

            string updateTimeLineApiPath = "/api/timeline/" + timeLineItem.TimeLineId;
            var updateTimeLineResponseString = await _httpClient.PutAsync(updateTimeLineApiPath, timeLineItem, new JsonMediaTypeFormatter());
            string returnString = await updateTimeLineResponseString.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<TimeLineItem>(returnString);
        }

        public async Task<bool> DeleteTimeLineItem(int timeLineItemId)
        {
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

            string timeLineApiPath = "/api/timeline/" + timeLineItemId;
            await _httpClient.DeleteAsync(timeLineApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel)
        {
            List<TimeLineItem> progenyTimeline = new List<TimeLineItem>();
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

            string timelineApiPath = "/api/timeline/progeny/" + progenyId + "?accessLevel=" + accessLevel;
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
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }

            string setChildApiPath = "/api/userinfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }
    }
}
