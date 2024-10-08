﻿using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.AspNetCore.Authentication;

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

        private class AccessTokenItem
        {
            public string AccessToken { get; init; } = string.Empty;
            public DateTime ExpiresIn { get; init; }
        }

        private readonly ConcurrentDictionary<string, AccessTokenItem> _accessTokens = new();

        public ApiTokenInMemoryClient(IOptions<AuthConfigurations> authConfigurations, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _authConfigurations = authConfigurations;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestVersion = new Version(2, 0);
            _logger = loggerFactory.CreateLogger<ApiTokenInMemoryClient>();
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
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
            if (!apiTokenOnly)
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");

            string accessToken = await GetApiToken(
            authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }
    }
}
