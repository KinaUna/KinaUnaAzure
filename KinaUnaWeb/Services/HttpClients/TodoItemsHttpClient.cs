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
    public class TodoItemsHttpClient : ITodoItemsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TodoItemsHttpClient(HttpClient httpClient, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, IConfiguration configuration)
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

        public async Task<TodoItem> GetTodoItem(int itemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            TodoItem todoItem = new();
            string todosApiPath = "/api/Todos/" + itemId;
            HttpResponseMessage todosResponse = await _httpClient.GetAsync(todosApiPath).ConfigureAwait(false);
            if (!todosResponse.IsSuccessStatusCode) return todoItem;

            string todoItemAsString = await todosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            todoItem = JsonConvert.DeserializeObject<TodoItem>(todoItemAsString);

            return todoItem;
        }

        public async Task<TodoItemsResponse> GetProgeniesTodoItemsList(TodoItemsRequest request)
        {
            TodoItemsResponse progenyTodoItemsResponse = new()
            {
                TodoItems = [],
                TodoItemsRequest = request,
                ProgenyList = []
            };
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string todosApiPath = "/api/Todos/Progenies/";
            HttpResponseMessage todosResponse =
                await _httpClient.PostAsync(todosApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!todosResponse.IsSuccessStatusCode) return progenyTodoItemsResponse;

            string todosResponseAsString = await todosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyTodoItemsResponse = JsonConvert.DeserializeObject<TodoItemsResponse>(todosResponseAsString);

            return progenyTodoItemsResponse;
        }

        public async Task<TodoItem> AddTodoItem(TodoItem todoItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string todosApiPath = "/api/Todos/";
            HttpResponseMessage todosResponse = await _httpClient.PostAsync(todosApiPath, new StringContent(JsonConvert.SerializeObject(todoItem), System.Text.Encoding.UTF8, "application/json"));
            if (!todosResponse.IsSuccessStatusCode) return new TodoItem();

            string todoAsString = await todosResponse.Content.ReadAsStringAsync();
            todoItem = JsonConvert.DeserializeObject<TodoItem>(todoAsString);
            return todoItem;
        }

        public async Task<TodoItem> UpdateTodoItem(TodoItem todoItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateApiPath = "/api/Todos/" + todoItem.TodoItemId;
            HttpResponseMessage todoResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(todoItem), System.Text.Encoding.UTF8, "application/json"));
            if (!todoResponse.IsSuccessStatusCode) return new TodoItem();

            string todoAsString = await todoResponse.Content.ReadAsStringAsync();
            todoItem = JsonConvert.DeserializeObject<TodoItem>(todoAsString);
            return todoItem;
        }

        public async Task<bool> DeleteTodoItem(int todoItemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string todoApiPath = "/api/Todos/" + todoItemId;
            HttpResponseMessage todoResponse = await _httpClient.DeleteAsync(todoApiPath);
            return todoResponse.IsSuccessStatusCode;
        }
    }
}
