using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Vocabulary API.
    /// </summary>
    public class WordsHttpClient : IWordsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenService _tokenService;

        public WordsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
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

        }

        /// <summary>
        /// Gets the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">The WordId of the VocabularyItem to get.</param>
        /// <returns>VocabularyItem object with the given WordId. If not found or an error occurs, a new VocabularyItem with WordId=0 is returned.</returns>
        public async Task<VocabularyItem> GetWord(int wordId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vocabularyApiPath = "/api/Vocabulary/" + wordId;
            HttpResponseMessage wordResponse = await _httpClient.GetAsync(vocabularyApiPath);
            if (!wordResponse.IsSuccessStatusCode) return new VocabularyItem();

            string wordAsString = await wordResponse.Content.ReadAsStringAsync();
            VocabularyItem wordItem = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
            return wordItem ?? new VocabularyItem();
        }

        /// <summary>
        /// Adds a new VocabularyItem.
        /// </summary>
        /// <param name="word">The new VocabularyItem to add.</param>
        /// <returns>The added VocabularyItem object.</returns>
        public async Task<VocabularyItem> AddWord(VocabularyItem word)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string vocabularyApiPath = "/api/Vocabulary/";
            HttpResponseMessage vocabularyResponse = await _httpClient.PostAsync(vocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (!vocabularyResponse.IsSuccessStatusCode) return new VocabularyItem();

            string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
            word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
            return word ?? new VocabularyItem();
        }

        /// <summary>
        /// Updates a VocabularyItem. The VocabularyItem with the same WordId will be updated.
        /// </summary>
        /// <param name="word">The VocabularyItem with the updated properties.</param>
        /// <returns>The updated VocabularyItem object. If the item is not found or an error occurs a new VocabularyItem with WordId=0 is returned.</returns>
        public async Task<VocabularyItem> UpdateWord(VocabularyItem word)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateVocabularyApiPath = "/api/Vocabulary/" + word.WordId;
            HttpResponseMessage vocabularyResponse = await _httpClient.PutAsync(updateVocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (!vocabularyResponse.IsSuccessStatusCode) return new VocabularyItem();

            string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
            word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
            return word ?? new VocabularyItem();
        }

        /// <summary>
        /// Deletes the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">The WordId of the VocabularyItem to delete.</param>
        /// <returns>bool: True if the VocabularyItem was successfully deleted.</returns>
        public async Task<bool> DeleteWord(int wordId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vocabularyApiPath = "/api/Vocabulary/" + wordId;
            HttpResponseMessage vocabularyResponse = await _httpClient.DeleteAsync(vocabularyApiPath);
            return vocabularyResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of all VocabularyItems for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        public async Task<List<VocabularyItem>> GetWordsList(int progenyId)
        {
            List<VocabularyItem> progenyWordsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string vocabularyApiPath = "/api/Vocabulary/Progeny/" + progenyId;
            HttpResponseMessage wordsResponse = await _httpClient.GetAsync(vocabularyApiPath);
            if (!wordsResponse.IsSuccessStatusCode) return progenyWordsList;

            string wordsAsString = await wordsResponse.Content.ReadAsStringAsync();
            progenyWordsList = JsonConvert.DeserializeObject<List<VocabularyItem>>(wordsAsString);
            return progenyWordsList;
        }
    }
}
