using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Services.HttpClients.Support
{
    public class HelpHttpClient : IHelpHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor = new HttpContextAccessor();

        /// <summary>
        /// Initializes a new instance of the HelpHttpClient class with the specified HTTP client, token service,
        /// environment, configuration, and HTTP context accessor.
        /// </summary>
        /// <remarks>The constructor configures the provided HttpClient with the appropriate base address
        /// and headers based on the current environment. The API endpoint is selected from configuration settings,
        /// allowing for environment-specific endpoints.</remarks>
        /// <param name="httpClient">The HttpClient instance used to send HTTP requests. The BaseAddress and default headers will be configured
        /// by this constructor.</param>
        /// <param name="tokenService">The token service used to acquire authentication tokens for API requests.</param>
        /// <param name="env">The host environment that determines which API base URL to use based on the current environment
        /// (development, staging, or production).</param>
        /// <param name="configuration">The application configuration used to retrieve API endpoint URLs.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor used to access the current HTTP context, if needed for request processing.</param>
        public HelpHttpClient(HttpClient httpClient, ITokenService tokenService, IHostEnvironment env, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;

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
        /// Retrieves help content for a specified page element and language.
        /// </summary>
        /// <param name="page">The name of the page for which to retrieve help content. Cannot be null or empty.</param>
        /// <param name="element">The identifier of the element on the page for which help content is requested. Cannot be null or empty.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be returned.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="HelpContent"/>
        /// object with the requested help information. Returns an empty <see cref="HelpContent"/> object if no content
        /// is found.</returns>
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

        /// <summary>
        /// Adds a new help content entry by sending it to the remote API.
        /// </summary>
        /// <remarks>The method requires the user to be authenticated. The returned HelpContent object
        /// reflects the data as stored by the remote API, which may include additional fields or
        /// modifications.</remarks>
        /// <param name="helpContent">The help content to add. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added help content as
        /// returned by the API. If the operation fails, returns an empty HelpContent object.</returns>
        public async Task<HelpContent> AddHelpContent(HelpContent helpContent)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Help/AddHelpContent", helpContent);
            if (response.IsSuccessStatusCode)
            {
                HelpContent addedHelpContent = await response.Content.ReadAsAsync<HelpContent>();
                if (addedHelpContent != null)
                {
                    return addedHelpContent;
                }
            }

            return new HelpContent();
        }

        /// <summary>
        /// Updates the specified help content by sending it to the server and returns the updated version.
        /// </summary>
        /// <remarks>The method requires the user to be authenticated. The returned HelpContent instance
        /// may be empty if the update operation is unsuccessful.</remarks>
        /// <param name="helpContent">The help content to update. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated help content
        /// returned by the server. If the update fails, returns a new, empty HelpContent instance.</returns>
        public async Task<HelpContent> UpdateHelpContent(HelpContent helpContent)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/Help/UpdateHelpContent", helpContent);
            if (response.IsSuccessStatusCode)
            {
                HelpContent updatedHelpContent = await response.Content.ReadAsAsync<HelpContent>();
                if (updatedHelpContent != null)
                {
                    return updatedHelpContent;
                }
            }

            return new HelpContent();
        }

        /// <summary>
        /// Deletes the help content item with the specified identifier.
        /// </summary>
        /// <remarks>The method requires the caller to be authenticated. If the specified help content
        /// does not exist or the deletion fails, an empty <see cref="HelpContent"/> object is returned.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content to delete.</param>
        /// <returns>A <see cref="HelpContent"/> object representing the deleted help content if the operation succeeds;
        /// otherwise, an empty <see cref="HelpContent"/> object.</returns>
        public async Task<HelpContent> DeleteHelpContent(int helpContentId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/Help/DeleteHelpContent?helpContentId={helpContentId}");
            if (response.IsSuccessStatusCode)
            {
                HelpContent deletedHelpContent = await response.Content.ReadAsAsync<HelpContent>();
                if (deletedHelpContent != null)
                {
                    return deletedHelpContent;
                }
            }

            return new HelpContent();
        }
    }
}
