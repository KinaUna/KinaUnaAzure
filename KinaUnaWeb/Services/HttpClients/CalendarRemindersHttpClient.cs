using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class CalendarRemindersHttpClient : ICalendarRemindersHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CalendarRemindersHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, ITokenService tokenService)
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
            _tokenService = tokenService;
        }

        public async Task<CalendarReminder> GetCalendarReminder(int reminderId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/AddCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(calendarReminder), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarReminder>(calendarReminderAsString);
        }

        public async Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/UpdateCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PutAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(calendarReminder), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CalendarReminder>(calendarReminderAsString);
        }

        public async Task<CalendarReminder> DeleteCalendarReminder(CalendarReminder calendarReminder)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

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

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string calendarRemindersApiPath = "/api/CalendarReminders/GetCalendarRemindersForUser";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new List<CalendarReminder>();

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CalendarReminder>>(calendarRemindersAsString);
        }

        public async Task<List<CalendarReminder>> GetUsersCalendarRemindersForEvent(int eventId, string userId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            CalendarRemindersForUserRequest request = new()
            {
                EventId = eventId,
                UserId = userId
            };

            string calendarRemindersApiPath = "/api/CalendarReminders/GetUsersCalendarRemindersForEvent/";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new List<CalendarReminder>();

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<CalendarReminder>>(calendarRemindersAsString);
        }
    }
}
