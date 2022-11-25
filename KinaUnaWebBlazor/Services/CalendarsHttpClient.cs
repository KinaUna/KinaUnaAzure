using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class CalendarsHttpClient: ICalendarsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;

        public CalendarsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _env = env;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer) ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
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

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer) ??
                                               throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");
            }

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration"));
            return accessToken;
        }

        public async Task<CalendarItem> GetCalendarItem(int eventId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            CalendarItem calendarItem = new CalendarItem();
            string calendarApiPath = "/api/Calendar/" + eventId;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(calendarAsString);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> AddCalendarItem(CalendarItem eventItem)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/";
            HttpResponseMessage calendarResponse = await _httpClient.PostAsync(calendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json"));
            if (calendarResponse.IsSuccessStatusCode)
            {
                string calendarItemAsString = await calendarResponse.Content.ReadAsStringAsync();
                eventItem = JsonConvert.DeserializeObject<CalendarItem>(calendarItemAsString);
                return eventItem;
            }

            return new CalendarItem();
        }

        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateCalendarApiPath = "/api/Calendar/" + eventItem.EventId;
            HttpResponseMessage updateCalendarResponse = await _httpClient.PutAsync(updateCalendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json"));
            if (updateCalendarResponse.IsSuccessStatusCode)
            {
                string updateCalendarItemAsString = await updateCalendarResponse.Content.ReadAsStringAsync();
                eventItem = JsonConvert.DeserializeObject<CalendarItem>(updateCalendarItemAsString);
                return eventItem;
            }

            return new CalendarItem();
        }

        public async Task<bool> DeleteCalendarItem(int eventId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/" + eventId;
            HttpResponseMessage calendarResponse = await _httpClient.DeleteAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);
            }

            return progenyCalendarList;
        }

        public async Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel)
        {
            List<CalendarItem> progenyCalendarList = new List<CalendarItem>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/Eventlist/" + progenyId + "/" + accessLevel;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);
            }

            return progenyCalendarList;
        }
    }
}
