using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class TimelineHttpClient : ITimelineHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public TimelineHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }

        public async Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string timeLineApiPath = "/api/Timeline/" + "GetTimelineItemByItemId/" + itemId + "/" + itemType;
            HttpResponseMessage timeLineResponse = await _httpClient.GetAsync(timeLineApiPath);
            if (!timeLineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timeLineResponse.Content.ReadAsStringAsync();
            TimeLineItem timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        public async Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string timeLineApiPath = "/api/Timeline/";
            HttpResponseMessage timelineResponse = await _httpClient.PostAsync(timeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timelineLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timelineLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateTimeLineApiPath = "/api/Timeline/" + timeLineItem.TimeLineId;
            HttpResponseMessage timelineResponse = await _httpClient.PutAsync(updateTimeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        public async Task<bool> DeleteTimeLineItem(int timeLineItemId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string timeLineApiPath = "/api/Timeline/" + timeLineItemId;
            HttpResponseMessage timelineResponse = await _httpClient.DeleteAsync(timeLineApiPath);
            return timelineResponse.IsSuccessStatusCode;
        }

        public async Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel, int order)
        {
            List<TimeLineItem> progenyTimeline = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string timelineApiPath = "/api/Timeline/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage timelineResponse = await _httpClient.GetAsync(timelineApiPath);
            if (!timelineResponse.IsSuccessStatusCode) return progenyTimeline;

            string timelineAsString = await timelineResponse.Content.ReadAsStringAsync();
            progenyTimeline = JsonConvert.DeserializeObject<List<TimeLineItem>>(timelineAsString);
            if (order == 1)
            {
                progenyTimeline = [.. progenyTimeline.OrderByDescending(t => t.ProgenyTime)];
            }
            else
            {
                progenyTimeline = [.. progenyTimeline.OrderBy(t => t.ProgenyTime)];
            }

            return progenyTimeline;
        }
    }
}
