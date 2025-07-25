using IdentityModel.Client;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the TimeLine API.
    /// </summary>
    public class TimelineHttpClient : ITimelineHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TimelineHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
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
        /// Gets the TimeLineItem with the given ItemId and ItemType.
        /// </summary>
        /// <param name="itemId">The ItemId (i.e. the PictureId, VideoId, ContactId, etc. that the TimeLineItem belongs to).</param>
        /// <param name="itemType">The ItemType of the TimeLineItem. Defined in the KinaUnaTypes.TimeLineType enum.</param>
        /// <returns>TimeLineItem object. If it cannot be found a new TimeLineItem with ItemId=0 is returned.</returns>
        public async Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string timeLineApiPath = "/api/Timeline/" + "GetTimelineItemByItemId/" + itemId + "/" + itemType;
            HttpResponseMessage timeLineResponse = await _httpClient.GetAsync(timeLineApiPath);
            if (!timeLineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timeLineResponse.Content.ReadAsStringAsync();
            TimeLineItem timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        /// <summary>
        /// Adds a new TimeLineItem.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem to add.</param>
        /// <returns>The added TimeLineItem object.</returns>
        public async Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string timeLineApiPath = "/api/Timeline/";
            HttpResponseMessage timelineResponse = await _httpClient.PostAsync(timeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timelineLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timelineLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        /// <summary>
        /// Updates a TimeLineItem. The TimeLineItem with the same TimeLineId will be updated.
        /// </summary>
        /// <param name="timeLineItem">The TimeLineItem with the updated properties.</param>
        /// <returns>The updated TimeLineItem object.</returns>
        public async Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateTimeLineApiPath = "/api/Timeline/" + timeLineItem.TimeLineId;
            HttpResponseMessage timelineResponse = await _httpClient.PutAsync(updateTimeLineApiPath, new StringContent(JsonConvert.SerializeObject(timeLineItem), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponse.IsSuccessStatusCode) return new TimeLineItem();

            string timeLineItemAsString = await timelineResponse.Content.ReadAsStringAsync();
            timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(timeLineItemAsString);
            return timeLineItem ?? new TimeLineItem();
        }

        /// <summary>
        /// Deletes the TimeLineItem with the given TimeLineId
        /// </summary>
        /// <param name="timeLineItemId">The TimeLineId of the TimeLineItem to delete.</param>
        /// <returns>bool: True if the TimeLineItem was successfully removed.</returns>
        public async Task<bool> DeleteTimeLineItem(int timeLineItemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string timeLineApiPath = "/api/Timeline/" + timeLineItemId;
            HttpResponseMessage timelineResponse = await _httpClient.DeleteAsync(timeLineApiPath);
            return timelineResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progeniesList">The list of Ids of the progenies to get TimeLineItems for.</param>
        /// <param name="order">Sort order: 0 for ascending, 1 for descending</param>
        /// <returns>List of TimeLineItem objects.</returns>
        public async Task<List<TimeLineItem>> GetProgeniesTimeline(List<int> progeniesList, int order)
        {
            List<TimeLineItem> progenyTimeline = [];

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string timelineApiPath = "/api/Timeline/Progenies/";
            HttpResponseMessage timelineResponse = await _httpClient.PostAsync(timelineApiPath, new StringContent(JsonConvert.SerializeObject(progeniesList), System.Text.Encoding.UTF8, "application/json"));
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

        public async Task<OnThisDayResponse> GetOnThisDayTimeLineItems(OnThisDayRequest onThisDayRequest)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string onThisDayApiPath = "/api/Timeline/GetOnThisDayTimeLineItems";
            HttpResponseMessage onThisDayResponse = await _httpClient.PostAsync(onThisDayApiPath, new StringContent(JsonConvert.SerializeObject(onThisDayRequest), System.Text.Encoding.UTF8, "application/json"));
            if (!onThisDayResponse.IsSuccessStatusCode) return new OnThisDayResponse();

            string onThisDayResponseAsString = await onThisDayResponse.Content.ReadAsStringAsync();
            OnThisDayResponse onThisDayResponseObject = JsonConvert.DeserializeObject<OnThisDayResponse>(onThisDayResponseAsString);
            return onThisDayResponseObject ?? new OnThisDayResponse();
        }

        public async Task<TimelineResponse> GetTimeLineData(TimelineRequest timelineRequest)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string timelineDataApiPath = "/api/Timeline/GetTimeLineRequestData";
            HttpResponseMessage timelineResponseMessage = await _httpClient.PostAsync(timelineDataApiPath, new StringContent(JsonConvert.SerializeObject(timelineRequest), System.Text.Encoding.UTF8, "application/json"));
            if (!timelineResponseMessage.IsSuccessStatusCode) return new TimelineResponse();

            string timelineResponseAsString = await timelineResponseMessage.Content.ReadAsStringAsync();
            TimelineResponse timelineResponse = JsonConvert.DeserializeObject<TimelineResponse>(timelineResponseAsString);
            return timelineResponse ?? new TimelineResponse();
        }
    }
}
