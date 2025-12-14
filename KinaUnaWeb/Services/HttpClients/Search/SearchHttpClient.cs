using KinaUna.Data;
using KinaUna.Data.Models.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace KinaUnaWeb.Services.HttpClients.Search
{
    /// <summary>
    /// Provides methods for searching entities via the Progeny API.
    /// </summary>
    public class SearchHttpClient : ISearchHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SearchHttpClient(HttpClient httpClient, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, IConfiguration configuration)
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
        }

        /// <summary>
        /// Searches calendar items by title, notes, location, and context.
        /// </summary>
        public async Task<SearchResponse<CalendarItem>> SearchCalendarItems(SearchRequest request)
        {
            SearchResponse<CalendarItem> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/CalendarItems";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<CalendarItem>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<CalendarItem> { SearchRequest = request };
        }

        /// <summary>
        /// Searches contacts by name fields, email, notes, website, context, and tags.
        /// </summary>
        public async Task<SearchResponse<Contact>> SearchContacts(SearchRequest request)
        {
            SearchResponse<Contact> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Contacts";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Contact>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Contact> { SearchRequest = request };
        }

        /// <summary>
        /// Searches friends by name, description, context, notes, and tags.
        /// </summary>
        public async Task<SearchResponse<Friend>> SearchFriends(SearchRequest request)
        {
            SearchResponse<Friend> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Friends";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Friend>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Friend> { SearchRequest = request };
        }

        /// <summary>
        /// Searches Kanban boards by title, description, tags, and context.
        /// </summary>
        public async Task<SearchResponse<KanbanBoard>> SearchKanbanBoards(SearchRequest request)
        {
            SearchResponse<KanbanBoard> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/KanbanBoards";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<KanbanBoard>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<KanbanBoard> { SearchRequest = request };
        }

        /// <summary>
        /// Searches locations by name, address fields, notes, and tags.
        /// </summary>
        public async Task<SearchResponse<Location>> SearchLocations(SearchRequest request)
        {
            SearchResponse<Location> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Locations";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Location>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Location> { SearchRequest = request };
        }

        /// <summary>
        /// Searches measurements by eye color and hair color.
        /// </summary>
        public async Task<SearchResponse<Measurement>> SearchMeasurements(SearchRequest request)
        {
            SearchResponse<Measurement> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Measurements";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Measurement>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Measurement> { SearchRequest = request };
        }

        /// <summary>
        /// Searches notes by title, content, and category.
        /// </summary>
        public async Task<SearchResponse<Note>> SearchNotes(SearchRequest request)
        {
            SearchResponse<Note> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Notes";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Note>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Note> { SearchRequest = request };
        }

        /// <summary>
        /// Searches pictures by tags and location.
        /// </summary>
        public async Task<SearchResponse<Picture>> SearchPictures(SearchRequest request)
        {
            SearchResponse<Picture> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Pictures";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Picture>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Picture> { SearchRequest = request };
        }

        /// <summary>
        /// Searches skills by name, description, and category.
        /// </summary>
        public async Task<SearchResponse<Skill>> SearchSkills(SearchRequest request)
        {
            SearchResponse<Skill> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Skills";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Skill>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Skill> { SearchRequest = request };
        }

        /// <summary>
        /// Searches sleep records by sleep notes.
        /// </summary>
        public async Task<SearchResponse<Sleep>> SearchSleepRecords(SearchRequest request)
        {
            SearchResponse<Sleep> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/SleepRecords";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Sleep>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Sleep> { SearchRequest = request };
        }

        /// <summary>
        /// Searches TodoItems by title, description, notes, tags, context, and location.
        /// </summary>
        public async Task<SearchResponse<TodoItem>> SearchTodoItems(SearchRequest request)
        {
            SearchResponse<TodoItem> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/TodoItems";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<TodoItem>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<TodoItem> { SearchRequest = request };
        }

        /// <summary>
        /// Searches vaccinations by name, description, and notes.
        /// </summary>
        public async Task<SearchResponse<Vaccination>> SearchVaccinations(SearchRequest request)
        {
            SearchResponse<Vaccination> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Vaccinations";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Vaccination>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Vaccination> { SearchRequest = request };
        }

        /// <summary>
        /// Searches videos by tags and location.
        /// </summary>
        public async Task<SearchResponse<Video>> SearchVideos(SearchRequest request)
        {
            SearchResponse<Video> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/Videos";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<Video>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<Video> { SearchRequest = request };
        }

        /// <summary>
        /// Searches vocabulary items by word, description, language, and sounds-like.
        /// </summary>
        public async Task<SearchResponse<VocabularyItem>> SearchVocabularyItems(SearchRequest request)
        {
            SearchResponse<VocabularyItem> response = new() { SearchRequest = request };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string searchApiPath = "/api/Search/VocabularyItems";
            HttpResponseMessage searchResponse = await _httpClient.PostAsync(searchApiPath,
                new StringContent(JsonSerializer.Serialize(request, JsonSerializerOptions.Web), System.Text.Encoding.UTF8, "application/json"));

            if (!searchResponse.IsSuccessStatusCode) return response;

            string responseAsString = await searchResponse.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<SearchResponse<VocabularyItem>>(responseAsString, JsonSerializerOptions.Web);

            return response ?? new SearchResponse<VocabularyItem> { SearchRequest = request };
        }
    }
}
