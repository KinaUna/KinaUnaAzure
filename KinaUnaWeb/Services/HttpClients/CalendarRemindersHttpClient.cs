using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

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
            string clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey);
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Local");
            }

            if (env.IsStaging())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Azure");
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
            HttpResponseMessage calendarResponse = await _httpClient.GetAsync(calendarApiPath);
            if (!calendarResponse.IsSuccessStatusCode) return calendarReminder;

            string calendarAsString = await calendarResponse.Content.ReadAsStringAsync();

            calendarReminder = JsonSerializer.Deserialize<CalendarReminder>(calendarAsString, JsonSerializerOptions.Web);

            return calendarReminder;
        }

        public async Task<CalendarReminder> AddCalendarReminder(CalendarReminder calendarReminder)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/AddCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonSerializer.Serialize(calendarReminder, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CalendarReminder>(calendarReminderAsString, JsonSerializerOptions.Web);
        }

        public async Task<CalendarReminder> UpdateCalendarReminder(CalendarReminder calendarReminder)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string calendarRemindersApiPath = "/api/CalendarReminders/UpdateCalendarReminder";
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PutAsync(calendarRemindersApiPath, new StringContent(JsonSerializer.Serialize(calendarReminder, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return new CalendarReminder();

            string calendarReminderAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<CalendarReminder>(calendarReminderAsString, JsonSerializerOptions.Web);
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

            return JsonSerializer.Deserialize<CalendarReminder>(calendarReminderAsString, JsonSerializerOptions.Web);
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
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return [];

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CalendarReminder>>(calendarRemindersAsString, JsonSerializerOptions.Web);
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
            HttpResponseMessage calendarRemindersResponse = await _httpClient.PostAsync(calendarRemindersApiPath, new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));
            if (!calendarRemindersResponse.IsSuccessStatusCode) return [];

            string calendarRemindersAsString = await calendarRemindersResponse.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<CalendarReminder>>(calendarRemindersAsString, JsonSerializerOptions.Web);
        }
    }
}
