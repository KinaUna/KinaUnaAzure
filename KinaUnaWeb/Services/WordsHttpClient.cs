using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class WordsHttpClient: IWordsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public WordsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        
        public async Task<VocabularyItem> GetWord(int wordId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/" + wordId;
            HttpResponseMessage wordResponse = await _httpClient.GetAsync(vocabularyApiPath);
            if (wordResponse.IsSuccessStatusCode)
            {
                string wordAsString = await wordResponse.Content.ReadAsStringAsync();
                VocabularyItem wordItem = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                if (wordItem != null)
                {
                    return wordItem;
                }
            }

            return new VocabularyItem();
        }

        public async Task<VocabularyItem> AddWord(VocabularyItem word)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string vocabularyApiPath = "/api/Vocabulary/";
            HttpResponseMessage vocabularyResponse = await _httpClient.PostAsync(vocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
                word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                if (word != null)
                {
                    return word;
                }
            }

            return new VocabularyItem();
        }

        public async Task<VocabularyItem> UpdateWord(VocabularyItem word)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateVocabularyApiPath = "/api/Vocabulary/" + word.WordId;
            HttpResponseMessage vocabularyResponse = await _httpClient.PutAsync(updateVocabularyApiPath, new StringContent(JsonConvert.SerializeObject(word), System.Text.Encoding.UTF8, "application/json"));
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                string wordAsString = await vocabularyResponse.Content.ReadAsStringAsync();
                word = JsonConvert.DeserializeObject<VocabularyItem>(wordAsString);
                if (word != null)
                {
                    return word;
                }
            }

            return new VocabularyItem();
        }

        public async Task<bool> DeleteWord(int wordId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            List<VocabularyItem> progenyWordsList = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
