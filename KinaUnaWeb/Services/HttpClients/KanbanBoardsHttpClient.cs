using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public class KanbanBoardsHttpClient : IKanbanBoardsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KanbanBoardsHttpClient(HttpClient httpClient, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env, IConfiguration configuration)
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

        public async Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanBoardsApiPath = "/api/KanbanBoards/";
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.PostAsJsonAsync(kanbanBoardsApiPath, kanbanBoard);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;

            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync();
            kanbanBoard = JsonSerializer.Deserialize<KanbanBoard>(kanbanBoardAsString, JsonSerializerOptions.Web);

            return kanbanBoard;
        }

        public async Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard kanbanBoard, bool hardDelete = false)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanBoardsApiPath = "/api/KanbanBoards/" + kanbanBoard.KanbanBoardId + "?hardDelete=" + hardDelete;
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.DeleteAsync(kanbanBoardsApiPath);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;

            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync();
            kanbanBoard = JsonSerializer.Deserialize<KanbanBoard>(kanbanBoardAsString, JsonSerializerOptions.Web);

            return kanbanBoard;
        }

        public async Task<KanbanBoard> GetKanbanBoard(int kanbanBoardId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            KanbanBoard kanbanBoard = new();
            string kanbanBoardsApiPath = "/api/KanbanBoards/GetKanbanBoard/" + kanbanBoardId;
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.GetAsync(kanbanBoardsApiPath);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;

            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync();
            kanbanBoard = JsonSerializer.Deserialize<KanbanBoard>(kanbanBoardAsString, JsonSerializerOptions.Web);

            return kanbanBoard;
        }

        public async Task<KanbanBoardsResponse> GetKanbanBoardsList(KanbanBoardsRequest kanbanBoardsRequest)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            KanbanBoardsResponse kanbanBoardsResponse = new();
            string kanbanBoardsApiPath = "/api/KanbanBoards/GetKanbanBoardsList";
            HttpResponseMessage kanbanBoardsListResponse = await _httpClient.PostAsJsonAsync(kanbanBoardsApiPath, kanbanBoardsRequest);
            if (!kanbanBoardsListResponse.IsSuccessStatusCode) return kanbanBoardsResponse;
            
            string kanbanBoardsListAsString = await kanbanBoardsListResponse.Content.ReadAsStringAsync();
            kanbanBoardsResponse = JsonSerializer.Deserialize<KanbanBoardsResponse>(kanbanBoardsListAsString, JsonSerializerOptions.Web);

            return kanbanBoardsResponse;
        }

        public async Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanBoardsApiPath = "/api/KanbanBoards/" + kanbanBoard.KanbanBoardId;
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.PutAsJsonAsync(kanbanBoardsApiPath, kanbanBoard);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;
            
            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync();
            kanbanBoard = JsonSerializer.Deserialize<KanbanBoard>(kanbanBoardAsString, JsonSerializerOptions.Web);

            return kanbanBoard;
        }
    }
}
