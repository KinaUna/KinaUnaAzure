using System.Net.Http.Headers;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class SkillsHttpClient: ISkillsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public SkillsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration.");

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

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration."));
            return accessToken;
        }

        
        public async Task<Skill?> GetSkill(int skillId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.GetAsync(skillsApiPath);
            if (skillResponse.IsSuccessStatusCode)
            {
                string skillAsString = await skillResponse.Content.ReadAsStringAsync();
                Skill? skillItem = JsonConvert.DeserializeObject<Skill>(skillAsString);
                return skillItem;
            }

            return new Skill();
        }

        public async Task<Skill?> AddSkill(Skill? skill)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/";
            HttpResponseMessage skillsResponse = await _httpClient.PostAsync(skillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (skillsResponse.IsSuccessStatusCode)
            {
                string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
                skill = JsonConvert.DeserializeObject<Skill>(skillsAsString);
                return skill;
            }

            return new Skill();
        }

        public async Task<Skill?> UpdateSkill(Skill? skill)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string updateSkillsApiPath = "/api/Skills/" + skill?.SkillId;
            HttpResponseMessage skillResponse = await _httpClient.PutAsync(updateSkillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (!skillResponse.IsSuccessStatusCode)
            {
                string skillAsString = await skillResponse.Content.ReadAsStringAsync();
                skill = JsonConvert.DeserializeObject<Skill>(skillAsString);
                return skill;
            }

            return new Skill();
        }

        public async Task<bool> DeleteSkill(int skillId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.DeleteAsync(skillsApiPath);
            if (skillResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Skill>?> GetSkillsList(int progenyId, int accessLevel)
        {
            List<Skill>? progenySkillsList = new List<Skill>();
            
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage skillsResponse = await _httpClient.GetAsync(skillsApiPath);
            if (skillsResponse.IsSuccessStatusCode)
            {
                string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
                progenySkillsList = JsonConvert.DeserializeObject<List<Skill>>(skillsAsString);
            }

            return progenySkillsList;
        }
    }
}
