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
    /// <summary>
    /// Provides methods to interact with the Skills API.
    /// </summary>
    public class SkillsHttpClient : ISkillsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SkillsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
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

        /// <summary>
        /// Gets the Skill with the given SkillId.
        /// </summary>
        /// <param name="skillId">The SkillId of the Skill to get.</param>
        /// <returns>The Skill object with the given SkillId. If the Skill cannot be found a new Skill object with SkillId is returned.</returns>
        public async Task<Skill> GetSkill(int skillId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.GetAsync(skillsApiPath);
            if (!skillResponse.IsSuccessStatusCode) return new Skill();

            string skillAsString = await skillResponse.Content.ReadAsStringAsync();
            Skill skillItem = JsonConvert.DeserializeObject<Skill>(skillAsString);
            return skillItem ?? new Skill();
        }

        /// <summary>
        /// Adds a new Skill.
        /// </summary>
        /// <param name="skill">The new Skill to add.</param>
        /// <returns>The added Skill object.</returns>
        public async Task<Skill> AddSkill(Skill skill)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string skillsApiPath = "/api/Skills/";
            HttpResponseMessage skillsResponse = await _httpClient.PostAsync(skillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (!skillsResponse.IsSuccessStatusCode) return new Skill();

            string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
            skill = JsonConvert.DeserializeObject<Skill>(skillsAsString);
            return skill ?? new Skill();
        }

        /// <summary>
        /// Updates a Skill. The Skill with the same SkillId will be updated.
        /// </summary>
        /// <param name="skill">The Skill with the updated properties.</param>
        /// <returns>The updated Skill object.</returns>
        public async Task<Skill> UpdateSkill(Skill skill)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateSkillsApiPath = "/api/Skills/" + skill.SkillId;
            HttpResponseMessage skillResponse = await _httpClient.PutAsync(updateSkillsApiPath, new StringContent(JsonConvert.SerializeObject(skill), System.Text.Encoding.UTF8, "application/json"));
            if (!skillResponse.IsSuccessStatusCode) return new Skill();

            string skillAsString = await skillResponse.Content.ReadAsStringAsync();
            skill = JsonConvert.DeserializeObject<Skill>(skillAsString);
            return skill;

        }

        /// <summary>
        /// Deletes the Skill with a given SkillId.
        /// </summary>
        /// <param name="skillId">The SkillId of the Skill to delete.</param>
        /// <returns>bool: True if the Skill was successfully removed.</returns>
        public async Task<bool> DeleteSkill(int skillId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string skillsApiPath = "/api/Skills/" + skillId;
            HttpResponseMessage skillResponse = await _httpClient.DeleteAsync(skillsApiPath);
            return skillResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets a list of all Skills for a Progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny to get Skills for.</param>
        /// <returns>List of Skill objects.</returns>
        public async Task<List<Skill>> GetSkillsList(int progenyId)
        {
            List<Skill> progenySkillsList = [];

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string skillsApiPath = "/api/Skills/Progeny/" + progenyId;
            HttpResponseMessage skillsResponse = await _httpClient.GetAsync(skillsApiPath);
            if (!skillsResponse.IsSuccessStatusCode) return progenySkillsList;

            string skillsAsString = await skillsResponse.Content.ReadAsStringAsync();
            progenySkillsList = JsonConvert.DeserializeObject<List<Skill>>(skillsAsString);

            return progenySkillsList;
        }
    }
}
