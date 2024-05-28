using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class SkillsHttpClient : ISkillsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public SkillsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }


        public async Task<Skill> GetSkill(int skillId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.GetAsync(skillsApiPath);
            if (!skillResponse.IsSuccessStatusCode) return new Skill();

            string skillAsString = await skillResponse.Content.ReadAsStringAsync();
            Skill skillItem = JsonConvert.DeserializeObject<Skill>(skillAsString);
            return skillItem ?? new Skill();
        }

        public async Task<Skill> AddSkill(Skill skill)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string skillsApiPath = "/api/Skills/";
            HttpResponseMessage skillsResponse = await _httpClient.PostAsync(skillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (!skillsResponse.IsSuccessStatusCode) return new Skill();

            string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
            skill = JsonConvert.DeserializeObject<Skill>(skillsAsString);
            return skill ?? new Skill();
        }

        public async Task<Skill> UpdateSkill(Skill skill)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateSkillsApiPath = "/api/Skills/" + skill.SkillId;
            HttpResponseMessage skillResponse = await _httpClient.PutAsync(updateSkillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (skillResponse.IsSuccessStatusCode) return new Skill();

            string skillAsString = await skillResponse.Content.ReadAsStringAsync();
            skill = JsonConvert.DeserializeObject<Skill>(skillAsString);
            return skill;

        }

        public async Task<bool> DeleteSkill(int skillId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.DeleteAsync(skillsApiPath);
            return skillResponse.IsSuccessStatusCode;
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId, int accessLevel)
        {
            List<Skill> progenySkillsList = [];

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string skillsApiPath = "/api/Skills/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage skillsResponse = await _httpClient.GetAsync(skillsApiPath);
            if (!skillsResponse.IsSuccessStatusCode) return progenySkillsList;

            string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
            progenySkillsList = JsonConvert.DeserializeObject<List<Skill>>(skillsAsString);

            return progenySkillsList;
        }
    }
}
