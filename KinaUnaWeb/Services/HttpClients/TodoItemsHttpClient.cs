using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public async Task<List<TodoItem>> GetProgeniesTodoItemsList(TodoItemsRequest request)
        {
            List<TodoItem> progenyTodoItemsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string todosApiPath = "/api/Todos/Progenies/";
            HttpResponseMessage todosResponse =
                await _httpClient.PostAsync(todosApiPath, new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (!todosResponse.IsSuccessStatusCode) return progenyTodoItemsList;

            string todosListAsString = await todosResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyTodoItemsList = JsonConvert.DeserializeObject<List<TodoItem>>(todosListAsString);

            return progenyTodoItemsList;
        }
    }
}
