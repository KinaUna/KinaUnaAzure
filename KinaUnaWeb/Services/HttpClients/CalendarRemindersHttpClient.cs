using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class CalendarRemindersHttpClient : ICalendarRemindersHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public CalendarRemindersHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<CalendarReminder> GetCalendarReminder(int reminderId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            CalendarReminder calendarReminder = new();
            string calendarApiPath = "/api/CalendarReminders/GetCalendarReminder/" + reminderId;
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (!calendarResponse.IsSuccessStatusCode) return calendarReminder;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            calendarReminder = JsonConvert.DeserializeObject<CalendarReminder>(calendarAsString);

            return calendarReminder;
        }

        public async Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/AddCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(calendarReminder), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarReminder>(calendarReminderAsString);
        }

        public async Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/UpdateCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PutAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(calendarReminder), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarReminder>(calendarReminderAsString);
        }

        public async Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarRemindersApiPath = "/api/CalendarReminders/DeleteCalendarReminder/" + calendarReminder.CalendarReminderId;
            HttpResponseMessage calendarRemindersResponse = await _httpClient.DeleteAsync(calendarRemindersApiPath);
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarReminder>(calendarReminderAsString);
        }

        public async Task<List<CalendarReminder>> GetCalendarRemindersForUser(string userId, bool filterNotified)
        {
            CalendarRemindersForUserRequest request = new()
            {
                UserId = userId,
                FilterNotified = filterNotified
            };

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarRemindersApiPath = "/api/CalendarReminders/GetCalendarRemindersForUser";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new List<CalendarReminder>();

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CalendarReminder>>(calendarRemindersAsString);
        }

        public async Task<List<CalendarReminder>> GetCalendarRemindersForEvent(int eventId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string calendarRemindersApiPath = "/api/CalendarReminders/GetCalendarRemindersForEvent/" + eventId;
            HttpResponseMessage calendarRemindersResponse = await _httpClient.GetAsync(calendarRemindersApiPath);
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new List<CalendarReminder>();

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CalendarReminder>>(calendarRemindersAsString);
        }
    }
}
