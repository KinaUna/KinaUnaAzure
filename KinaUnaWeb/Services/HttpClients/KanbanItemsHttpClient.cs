using Duende.IdentityModel.Client;
using KinaUna.Data;
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
    public class KanbanItemsHttpClient: IKanbanItemsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KanbanItemsHttpClient(HttpClient httpClient, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, IConfiguration configuration)
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

        public async Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanItemsApiPath = "/api/KanbanItems/";
            HttpResponseMessage kanbanItemsResponse = await _httpClient.PostAsJsonAsync(kanbanItemsApiPath, kanbanItem).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItem;
            
            string kanbanItemAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanItem = JsonConvert.DeserializeObject<KanbanItem>(kanbanItemAsString);
            
            return kanbanItem;
        }

        public async Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanItemsApiPath = "/api/KanbanItems/" + kanbanItem.KanbanItemId;
            HttpResponseMessage kanbanItemsResponse = await _httpClient.DeleteAsync(kanbanItemsApiPath).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItem;

            string kanbanItemAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanItem = JsonConvert.DeserializeObject<KanbanItem>(kanbanItemAsString);

            return kanbanItem;
        }

        public async Task<KanbanItem> GetKanbanItem(int kanbanItemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            KanbanItem kanbanItem = new();
            string kanbanItemsApiPath = "/api/KanbanItems/GetKanbanItem/" + kanbanItemId;
            HttpResponseMessage kanbanItemsResponse = await _httpClient.GetAsync(kanbanItemsApiPath).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItem;

            string kanbanItemAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            kanbanItem = JsonConvert.DeserializeObject<KanbanItem>(kanbanItemAsString);

            return kanbanItem;
        }

        public async Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<KanbanItem> kanbanItems = [];
            string kanbanItemsApiPath = "/api/KanbanItems/GetKanbanItemsForBoard/" + kanbanBoardId + "/false";
            HttpResponseMessage kanbanItemsResponse = await _httpClient.GetAsync(kanbanItemsApiPath).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItems;

            string kanbanItemsAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            kanbanItems = JsonConvert.DeserializeObject<List<KanbanItem>>(kanbanItemsAsString);

            return kanbanItems;
        }

        public async Task<List<KanbanItem>> GetKanbanItemsForTodoItem(int todoItemId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            List<KanbanItem> kanbanItems = [];
            string kanbanItemsApiPath = "/api/KanbanItems/GetKanbanItemsForTodoItem/" + todoItemId + "/false";
            HttpResponseMessage kanbanItemsResponse = await _httpClient.GetAsync(kanbanItemsApiPath).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItems;

            string kanbanItemsAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            kanbanItems = JsonConvert.DeserializeObject<List<KanbanItem>>(kanbanItemsAsString);

            return kanbanItems;
        }

        public async Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanItemsApiPath = "/api/KanbanItems/" + kanbanItem.KanbanItemId;
            HttpResponseMessage kanbanItemsResponse = await _httpClient.PutAsJsonAsync(kanbanItemsApiPath, kanbanItem).ConfigureAwait(false);
            if (!kanbanItemsResponse.IsSuccessStatusCode) return kanbanItem;

            string kanbanItemAsString = await kanbanItemsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanItem = JsonConvert.DeserializeObject<KanbanItem>(kanbanItemAsString);

            return kanbanItem;
        }
    }
}
