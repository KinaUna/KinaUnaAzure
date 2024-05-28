using System.Collections.Concurrent;
using IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace KinaUnaWebBlazor.Services
{
    // Original source: https://github.com/damienbod/AspNetCoreHybridFlowWithApi/blob/master/WebMVCClient/ApiTokenInMemoryClient.cs

    public class AuthConfigurations
    {
        public string StsServer { get; set; } = string.Empty;
        public string ProtectedApiUrl { get; set; } = string.Empty;
    }

    public class ApiTokenInMemoryClient
    {
        private readonly ILogger<ApiTokenInMemoryClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly IOptions<AuthConfigurations> _authConfigurations;

        private class AccessTokenItem
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresIn { get; set; }
        }

        private readonly ConcurrentDictionary<string, AccessTokenItem> _accessTokens = new();

        public ApiTokenInMemoryClient(IOptions<AuthConfigurations> authConfigurations, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _authConfigurations = authConfigurations;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestVersion = new Version(2, 0);
            _logger = loggerFactory.CreateLogger<ApiTokenInMemoryClient>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
        public async Task<string> GetApiToken(string apiName, string apiScope, string secret)
        {
            if (_accessTokens.ContainsKey(apiName))
            {
                AccessTokenItem? accessToken = _accessTokens.GetValueOrDefault(apiName);
                if (accessToken != null)
                {
                    if (accessToken.ExpiresIn > DateTime.UtcNow)
                    {
                        return accessToken.AccessToken;
                    }

                    // remove
                    _accessTokens.TryRemove(apiName, out AccessTokenItem? _);
                }
            }

            _logger.LogDebug($"GetApiToken new from STS for {apiName}");

            // add
            AccessTokenItem newAccessToken = await GetApiTokenInternal(apiName, apiScope, secret);
            _accessTokens.TryAdd(apiName, newAccessToken);

            return newAccessToken.AccessToken;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
        private async Task<AccessTokenItem> GetApiTokenInternal(string apiName, string apiScope, string secret)
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

                if (tokenResponse.IsError)
                {
                    _logger.LogError($"tokenResponse.IsError Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");
                    throw new ApplicationException($"Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");
                }

                return new AccessTokenItem
                {
                    ExpiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    AccessToken = tokenResponse.AccessToken
                };

            }
            catch (Exception e)
            {
                _logger.LogError($"Exception {e}");
                throw new ApplicationException($"Exception {e}");
            }
        }
    }
}
