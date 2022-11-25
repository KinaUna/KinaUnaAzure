using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class WordsHttpClient: IWordsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;

        public WordsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _env = env;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer) ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration");
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext? currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ??
                                                  throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer) ??
                                               throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");
            }

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration."));
            return accessToken;
        }

        public async Task<VocabularyItem> GetWord(int wordId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/" + wordId;
            HttpResponseMessage wordResponse = await _httpClient.GetAsync(vocabularyApiPath);
            if (wordResponse.IsSuccessStatusCode)
            {
                string wordAsString = await wordResponse.Content.ReadAsStringAsync();
                VocabularyItem wordItem = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                if (wordItem.WordId != 0)
                {
                    return wordItem;
                }
            }

            return new VocabularyItem();
        }

        public async Task<VocabularyItem> AddWord(VocabularyItem word)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/";
            HttpResponseMessage vocabularyResponse = await _httpClient.PostAsync(vocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
                word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                return word;
            }

            return new VocabularyItem();
        }

        public async Task<VocabularyItem> UpdateWord(VocabularyItem word)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateVocabularyApiPath = "/api/Vocabulary/" + word.WordId;
            HttpResponseMessage vocabularyResponse = await _httpClient.PutAsync(updateVocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
                word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                return word;
            }

            return new VocabularyItem();
        }

        public async Task<bool> DeleteWord(int wordId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/" + wordId;
            HttpResponseMessage vocabularyResponse = await _httpClient.DeleteAsync(vocabularyApiPath);
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<VocabularyItem>> GetWordsList(int progenyId, int accessLevel)
        {
            List<VocabularyItem> progenyWordsList = new List<VocabularyItem>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage wordsResponse = await _httpClient.GetAsync(vocabularyApiPath);
            if (wordsResponse.IsSuccessStatusCode)
            {
                string wordsAsString = await wordsResponse.Content.ReadAsStringAsync();
                progenyWordsList = JsonConvert.DeserializeObject<List<VocabularyItem>>(wordsAsString);
            }

            return progenyWordsList;
        }
    }
}
