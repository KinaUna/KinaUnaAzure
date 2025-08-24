using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public class SubtasksHttpClient : ISubtasksHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SubtasksHttpClient(HttpClient httpClient, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, IConfiguration configuration)
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

        public async Task<TodoItem> GetSubtask(int itemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            TodoItem todoItem = new();
            string subtasksApiPath = "/api/Subtasks/" + itemId;
            HttpResponseMessage todosResponse = await _httpClient.GetAsync(subtasksApiPath).ConfigureAwait(false);
            if (!todosResponse.IsSuccessStatusCode) return todoItem;

            string todoItemAsString = await todosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            todoItem = JsonConvert.DeserializeObject<TodoItem>(todoItemAsString);

            return todoItem;
        }

        public async Task<SubtasksResponse> GetSubtasksList(SubtasksRequest request)
        {
            SubtasksResponse progenySubtasksResponse = new()
            {
                Subtasks = [],
                SubtasksRequest = request,
            };

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string subtasksApiPath = "/api/Subtasks/GetSubtasksForTodoItem/";
            HttpResponseMessage subtasksResponse =
                await _httpClient.PostAsync(subtasksApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!subtasksResponse.IsSuccessStatusCode) return progenySubtasksResponse;

            string subtasksResponseAsString = await subtasksResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenySubtasksResponse = JsonConvert.DeserializeObject<SubtasksResponse>(subtasksResponseAsString);

            return progenySubtasksResponse;
        }

        public async Task<TodoItem> AddSubtask(TodoItem subtask)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string subtasksApiPath = "/api/Subtasks/";
            HttpResponseMessage subtasksResponse = await _httpClient.PostAsync(subtasksApiPath, new StringContent(JsonConvert.SerializeObject(subtask), System.Text.Encoding.UTF8, "application/json"));
            if (!subtasksResponse.IsSuccessStatusCode) return new TodoItem();

            string subtaskAsString = await subtasksResponse.Content.ReadAsStringAsync();
            subtask = JsonConvert.DeserializeObject<TodoItem>(subtaskAsString);
            return subtask;
        }

        public async Task<TodoItem> UpdateSubtask(TodoItem subtask)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateApiPath = "/api/Subtasks/" + subtask.TodoItemId;
            HttpResponseMessage subtasksResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(subtask), System.Text.Encoding.UTF8, "application/json"));
            if (!subtasksResponse.IsSuccessStatusCode) return new TodoItem();

            string subtaskAsString = await subtasksResponse.Content.ReadAsStringAsync();
            subtask = JsonConvert.DeserializeObject<TodoItem>(subtaskAsString);
            return subtask;
        }

        public async Task<bool> DeleteSubtask(int subtaskId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string subtaskApiPath = "/api/Subtasks/" + subtaskId;
            HttpResponseMessage subtaskResponse = await _httpClient.DeleteAsync(subtaskApiPath);
            return subtaskResponse.IsSuccessStatusCode;
        }
    }
}
