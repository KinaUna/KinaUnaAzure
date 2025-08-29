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
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.PostAsJsonAsync(kanbanBoardsApiPath, kanbanBoard).ConfigureAwait(false);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;

            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanBoard = JsonConvert.DeserializeObject<KanbanBoard>(kanbanBoardAsString);

            return kanbanBoard;
        }

        public async Task<KanbanBoard> GetKanbanBoard(int kanbanBoardId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            KanbanBoard kanbanBoard = new();
            string kanbanBoardsApiPath = "/api/KanbanBoards/" + kanbanBoardId;
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.GetAsync(kanbanBoardsApiPath).ConfigureAwait(false);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;

            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanBoard = JsonConvert.DeserializeObject<KanbanBoard>(kanbanBoardAsString);

            return kanbanBoard;
        }

        public async Task<KanbanBoardsResponse> GetProgeniesKanbanBoardsList(KanbanBoardsRequest kanbanBoardsRequest)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            KanbanBoardsResponse kanbanBoardsResponse = new();
            string kanbanBoardsApiPath = "/api/KanbanBoards/GetProgeniesKanbanBoardsList";
            HttpResponseMessage kanbanBoardsListResponse = await _httpClient.PostAsJsonAsync(kanbanBoardsApiPath, kanbanBoardsRequest).ConfigureAwait(false);
            if (!kanbanBoardsListResponse.IsSuccessStatusCode) return kanbanBoardsResponse;
            
            string kanbanBoardsListAsString = await kanbanBoardsListResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanBoardsResponse = JsonConvert.DeserializeObject<KanbanBoardsResponse>(kanbanBoardsListAsString);

            return kanbanBoardsResponse;
        }

        public async Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string kanbanBoardsApiPath = "/api/KanbanBoards/" + kanbanBoard.KanbanBoardId;
            HttpResponseMessage kanbanBoardsResponse = await _httpClient.PutAsJsonAsync(kanbanBoardsApiPath, kanbanBoard).ConfigureAwait(false);
            if (!kanbanBoardsResponse.IsSuccessStatusCode) return kanbanBoard;
            
            string kanbanBoardAsString = await kanbanBoardsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            kanbanBoard = JsonConvert.DeserializeObject<KanbanBoard>(kanbanBoardAsString);

            return kanbanBoard;
        }
    }
}
