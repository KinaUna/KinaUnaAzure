using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients.Support
{
    public class HelpHttpClient: IHelpHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        
        public HelpHttpClient(HttpClient httpClient, ITokenService tokenService, IHostEnvironment env, IConfiguration configuration)
        {
            _httpClient = httpClient;
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

        public async Task<HelpContent> GetHelpContent(string page, string element, int languageId)
        {
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(string.Empty);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            HttpResponseMessage response = await _httpClient.GetAsync($"/api/Help/GetHelpContent?page={page}&element={element}&languageId={languageId}");
            if (response.IsSuccessStatusCode)
            {
                HelpContent helpContent = await response.Content.ReadAsAsync<HelpContent>();
                if (helpContent != null)
                {
                    return helpContent;
                }
            }
            return new HelpContent();
        }
    }
}
