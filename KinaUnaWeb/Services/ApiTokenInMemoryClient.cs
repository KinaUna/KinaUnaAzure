using IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    // Original source: https://github.com/damienbod/AspNetCoreHybridFlowWithApi/blob/master/WebMVCClient/ApiTokenInMemoryClient.cs

    public class AuthConfigurations
    {
        public string StsServer { get; set; }
        public string ProtectedApiUrl { get; set; }
    }
    /// <summary>
    /// Provides access tokens for the Progeny API and the Media API.
    /// Uses a ConcurrentDictionary to store the access tokens in memory.
    /// For dependency injection, configure as a singleton.
    /// </summary>
    public class ApiTokenInMemoryClient
    {
        private readonly ILogger<ApiTokenInMemoryClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly IOptions<AuthConfigurations> _authConfigurations;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;

        private class AccessTokenItem
        {
            public string AccessToken { get; init; } = string.Empty;
            public DateTime ExpiresIn { get; init; }
        }

        private readonly ConcurrentDictionary<string, AccessTokenItem> _accessTokens = new();

        public ApiTokenInMemoryClient(IOptions<AuthConfigurations> authConfigurations, IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration,
            IHostEnvironment env)
        {
            _authConfigurations = authConfigurations;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestVersion = new Version(2, 0);
            _logger = loggerFactory.CreateLogger<ApiTokenInMemoryClient>();
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _env = env;
        }

        /// <summary>
        /// Get an access token for a given API.
        /// </summary>
        /// <param name="apiName">The API's name. Defined in IDP/Config.cs : GetApiResources.</param>
        /// <param name="apiScope">The scope name(s) of the api(s). Multiple scopes can be used, separated by space. Defined in IDP/Config.cs : ApiScopes.</param>
        /// <param name="secret">The client secret for the authentication server. Defined in IDP/Config.cs : GetClients</param>
        /// <returns>String with the access token.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
        private async Task<string> GetApiToken(string apiName, string apiScope, string secret)
        {
            if (_accessTokens.ContainsKey(apiName))
            {
                AccessTokenItem accessToken = _accessTokens.GetValueOrDefault(apiName);
                if (accessToken != null)
                {
                    if (accessToken.ExpiresIn > DateTime.UtcNow)
                    {
                        return accessToken.AccessToken;
                    }

                    // remove
                    _accessTokens.TryRemove(apiName, out AccessTokenItem _);
                }
            }

            _logger.LogDebug($"GetApiToken new from STS for {apiName}");

            // add
            AccessTokenItem newAccessToken = await GetNewApiToken(apiName, apiScope, secret);
            _accessTokens.TryAdd(apiName, newAccessToken);

            return newAccessToken.AccessToken;
        }

        /// <summary>
        /// Get a new access token for a given API.
        /// </summary>
        /// <param name="apiName">The API's name. Defined in IDP/Config.cs : GetApiResources.</param>
        /// <param name="apiScope">The scope name(s) of the api(s). Multiple scopes can be used, separated by space. Defined in IDP/Config.cs : ApiScopes.</param>
        /// <param name="secret">The client secret for the authentication server. Defined in IDP/Config.cs : GetClients</param>
        /// <returns>The AccessTokenItem.</returns>
        /// <exception cref="ApplicationException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
        private async Task<AccessTokenItem> GetNewApiToken(string apiName, string apiScope, string secret)
        {
            try
            {
                DiscoveryDocumentResponse disco = await _httpClient.GetDiscoveryDocumentAsync(_authConfigurations.Value.StsServer);

                if (disco.IsError)
                {
                    _logger.LogError($"disco error Status code: {disco.IsError}, Error: {disco.Error}");
                    throw new ApplicationException($"Status code: {disco.IsError}, Error: {disco.Error}");
                }

                TokenResponse tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Scope = apiScope,
                    ClientSecret = secret,
                    Address = disco.TokenEndpoint,
                    ClientId = apiName
                });

                if (!tokenResponse.IsError)
                    return new AccessTokenItem
                    {
                        ExpiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                        AccessToken = tokenResponse.AccessToken
                    };

                _logger.LogError($"tokenResponse.IsError Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");
                throw new ApplicationException($"Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");

            }
            catch (Exception e)
            {
                _logger.LogError($"Exception {e}");
                throw new ApplicationException($"Exception {e}");
            }
        }

        /// <summary>
        /// Get an access token for the Progeny API.
        /// </summary>
        /// <param name="apiTokenOnly">If true gets access token from the current context. Use only if the current user identity is irrelevant for the Api endpoint.</param>
        /// <returns>String with the access token.</returns>
        public async Task<string> GetProgenyAndMediaApiToken(bool apiTokenOnly = false)
        {
            string accessToken;
            string authenticationServer;
            string authenticationServerClientId;
            string authenticationServerSecret;
            string scope = Constants.ProgenyApiName;

            if (_env.IsDevelopment())
            {
                authenticationServer = _configuration.GetValue<string>("AuthenticationServerLocal");
                authenticationServerClientId = _configuration.GetValue<string>("WebServerClientIdLocal");
                authenticationServerSecret = _configuration.GetValue<string>("OpenIddictSecretStringLocal");
                scope = Constants.ProgenyApiName + "local";
            }
            else
            {
                authenticationServer = _configuration.GetValue<string>("AuthenticationServer");
                authenticationServerClientId = _configuration.GetValue<string>("WebServerClientId");
                authenticationServerSecret = _configuration.GetValue<string>("OpenIddictSecretString");
            }

            if (!apiTokenOnly)
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    string userAccessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

                    if (!string.IsNullOrWhiteSpace(userAccessToken))
                    {
                        // Exchange user token for API token
                        TokenResponse tokenExchangeResponse = await _httpClient.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
                        {
                            Address = authenticationServer + "/connect/token",
                            ClientId = authenticationServerClientId,
                            ClientSecret = authenticationServerSecret,
                            SubjectToken = userAccessToken,
                            SubjectTokenType = "urn:ietf:params:oauth:token-type:access_token",
                            Scope = scope
                        });

                        accessToken = tokenExchangeResponse.AccessToken;
                        return accessToken;
                    }
                }
            }

            // No user: use client credentials
            TokenResponse tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = authenticationServer + "/connect/token",
                ClientId = authenticationServerClientId,
                ClientSecret = authenticationServerSecret,
                Scope = scope
            });

            accessToken = tokenResponse.AccessToken;

            return accessToken;
        }
    }
}
