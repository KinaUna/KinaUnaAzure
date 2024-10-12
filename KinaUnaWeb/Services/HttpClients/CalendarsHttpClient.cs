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
    /// <summary>
    /// Provides methods for interacting with the Calendar API.
    /// </summary>
    public class CalendarsHttpClient : ICalendarsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public CalendarsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets a CalendarItem with a given EventId.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem object.</param>
        /// <returns>CalendarItem. Start and end times are in UTC timezone.</returns>
        public async Task<CalendarItem> GetCalendarItem(int eventId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            CalendarItem calendarItem = new();
            string calendarApiPath = "/api/Calendar/" + eventId;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (!calendarResponse.IsSuccessStatusCode) return calendarItem;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            calendarItem = JsonConvert.DeserializeObject<CalendarItem>(calendarAsString);

            return calendarItem;
        }

        /// <summary>
        /// Adds a new CalendarItem object.
        /// </summary>
        /// <param name="eventItem">The CalendarItem object to be added. Start and end times should be in UTC timezone.</param>
        /// <returns>The CalendarItem object that was added. Start and end times are in UTC timezone.</returns>
        public async Task<CalendarItem> AddCalendarItem(CalendarItem eventItem)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string calendarApiPath = "/api/Calendar/";
            HttpResponseMessage calendarResponse = await _httpClient.PostAsync(calendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarResponse.IsSuccessStatusCode) return new CalendarItem();

            string calendarItemAsString = await calendarResponse.Content.ReadAsStringAsync();
            eventItem = JsonConvert.DeserializeObject<CalendarItem>(calendarItemAsString);
            return eventItem;

        }

        /// <summary>
        /// Updates a CalendarItem object.
        /// </summary>
        /// <param name="eventItem">The CalendarItem object to be updated. Start and end times should be in UTC timezone.</param>
        /// <returns>The updated CalendarItem object.Start and end times are in UTC timezone.</returns>
        public async Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateCalendarApiPath = "/api/Calendar/" + eventItem.EventId;
            HttpResponseMessage updateCalendarResponse = await _httpClient.PutAsync(updateCalendarApiPath, new StringContent(JsonConvert.SerializeObject(eventItem), System.Text.Encoding.UTF8, "application/json"));
            if (!updateCalendarResponse.IsSuccessStatusCode) return new CalendarItem();

            string updateCalendarItemAsString = await updateCalendarResponse.Content.ReadAsStringAsync();
            eventItem = JsonConvert.DeserializeObject<CalendarItem>(updateCalendarItemAsString);
            return eventItem;

        }

        /// <summary>
        /// Removes the CalendarItem object with a given EventId.
        /// </summary>
        /// <param name="eventId">The EventId of the CalendarItem to remove.</param>
        /// <returns>bool: True if the CalendarItem object was successfully removed.</returns>
        public async Task<bool> DeleteCalendarItem(int eventId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/" + eventId;
            HttpResponseMessage calendarResponse = await _httpClient.DeleteAsync(calendarApiPath).ConfigureAwait(false);
            return calendarResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get the list of CalendarItems for.</param>
        /// <returns>List of CalendarItem objects. Start and end times are in UTC timezone.</returns>
        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> progenyCalendarList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/Progeny/" + progenyId;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (!calendarResponse.IsSuccessStatusCode) return progenyCalendarList;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);

            return progenyCalendarList;
        }

        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyIds">The list of Ids of the Progenies to get the list of CalendarItems for.</param>
        /// <returns>List of CalendarItem objects. Start and end times are in UTC timezone.</returns>
        public async Task<List<CalendarItem>> GetProgeniesCalendarList(List<int> progenyIds)
        {
            List<CalendarItem> progenyCalendarList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/Progenies/";
            HttpResponseMessage calendarResponse = await _httpClient.PostAsync(calendarApiPath, new StringContent(JsonConvert.SerializeObject(progenyIds), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!calendarResponse.IsSuccessStatusCode) return progenyCalendarList;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);

            return progenyCalendarList;
        }

        /// <summary>
        /// Gets the next 5 upcoming events in the progeny's calendar.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get CalendarItems for.</param>
        /// <param name="timeZone">The user's time zone.</param>
        /// <returns>List of CalendarItem objects. Start and end times are in the user's timezone.</returns>
        public async Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, string timeZone)
        {
            List<CalendarItem> progenyCalendarList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarApiPath = "/api/Calendar/Eventlist/" + progenyId;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (!calendarResponse.IsSuccessStatusCode) return progenyCalendarList;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyCalendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarAsString);

            if (progenyCalendarList.Count == 0) return progenyCalendarList;

            foreach (CalendarItem eventItem in progenyCalendarList)
            {
                if (!eventItem.StartTime.HasValue || !eventItem.EndTime.HasValue) continue;

                eventItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            return progenyCalendarList;
        }
    }
}
