using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class TimelineHttpClient: ITimelineHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public TimelineHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
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

        public async Task<TimeLineItem?> GetTimeLineItem(string itemId, int itemType)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string timeLineApiPath = "/api/Timeline/" + "GetTimelineItemByItemId/" + itemId + "/" + itemType;
            HttpResponseMessage timeLineResponse = await _httpClient.GetAsync(timeLineApiPath);
            if (!timeLineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timeLineResponse.Content.ReadAsStringAsync();
            TimeLineItem? timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem;

        }

        public async Task<TimeLineItem?> AddTimeLineItem(TimeLineItem? timeLineItem)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            const string timeLineApiPath = "/api/Timeline/";
            HttpResponseMessage timelineResponse = await _httpClient.PostAsync(timeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timelineLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timelineLineItemAsString);
            return timeLineItem;

        }

        public async Task<TimeLineItem?> UpdateTimeLineItem(TimeLineItem? timeLineItem)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateTimeLineApiPath = "/api/Timeline/" + timeLineItem?.TimeLineId;
            HttpResponseMessage timelineResponse = await _httpClient.PutAsync(updateTimeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem;

        }

        public async Task<bool> DeleteTimeLineItem(int timeLineItemId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string timeLineApiPath = "/api/Timeline/" + timeLineItemId;
            HttpResponseMessage timelineResponse = await _httpClient.DeleteAsync(timeLineApiPath);
            return timelineResponse.IsSuccessStatusCode;
        }

        public async Task<List<TimeLineItem>?> GetTimeline(int progenyId, int accessLevel)
        {
            List<TimeLineItem>? progenyTimeline = [];
            
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string timelineApiPath = "/api/Timeline/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage timelineResponse = await _httpClient.GetAsync(timelineApiPath);
            if (!timelineResponse.IsSuccessStatusCode) return progenyTimeline;

            string timelineAsString = await timelineResponse.Content.ReadAsStringAsync();
            progenyTimeline = JsonConvert.DeserializeObject<List<TimeLineItem>>(timelineAsString);

            return progenyTimeline;
        }
    }
}
