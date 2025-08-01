
using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using OpenIddict.Abstractions;
using System.Security.Authentication;

namespace KinaUna.OpenIddict.Services
{

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _authenticationServer;
        private readonly string _authApiClientId;
        private readonly string _authenticationServerSecret;
        private readonly string _scope;
        public TokenService(IHttpClientFactory httpClientFactory, IHostEnvironment env, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;

            string scope = OpenIddictConstants.Scopes.OfflineAccess + " " +
                           OpenIddictConstants.Scopes.Email + " " +
                           OpenIddictConstants.Scopes.Profile + " " +
                           OpenIddictConstants.Scopes.Roles;


            if (env.IsDevelopment())
            {
                _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local") ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Local was not found in the configuration data.");
                _authApiClientId = configuration.GetValue<string>(AuthConstants.AuthApiOnlyClientIdKey + "Local") ?? throw new InvalidOperationException(AuthConstants.AuthApiOnlyClientIdKey + "Local was not found in the configuration data.");
                _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Local") ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Local was not found in the configuration data.");
                _scope = scope + " " + AuthConstants.ProgenyApiName + "local " + AuthConstants.AuthApiName + "local";
            }
            else
            {
                if (env.IsStaging())
                {
                    _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure") ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + "Azure was not found in the configuration data.");
                    _authApiClientId = configuration.GetValue<string>(AuthConstants.AuthApiOnlyClientIdKey + "Azure") ?? throw new InvalidOperationException(AuthConstants.AuthApiOnlyClientIdKey + "Azure was not found in the configuration data.");
                    _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey + "Azure") ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + "Azure was not found in the configuration data.");
                    _scope = scope + " " + AuthConstants.ProgenyApiName + "azure " + AuthConstants.AuthApiName + "azure";
                }
                else
                {
                    _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey) ?? throw new InvalidOperationException(AuthConstants.AuthenticationServerUrlKey + " was not found in the configuration data.");
                    _authApiClientId = configuration.GetValue<string>(AuthConstants.AuthApiOnlyClientIdKey) ?? throw new InvalidOperationException(AuthConstants.AuthApiOnlyClientIdKey + " was not found in the configuration data.");
                    _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.AuthServerClientSecretKey) ?? throw new InvalidOperationException(AuthConstants.AuthServerClientSecretKey + " was not found in the configuration data.");
                    _scope = scope + " " + AuthConstants.ProgenyApiName + " " + AuthConstants.AuthApiName;
                }
            }
        }

        public async Task<TokenInfo> GetValidTokenAsync()
        {
            TokenInfo apiTokenInfo = await ApiTokenAsync();
            
            return apiTokenInfo;
        }

        
        /// <summary>
        /// Asynchronously retrieves an access token using client credentials.
        /// </summary>
        /// <remarks>This method requests an access token from the authentication server using the client
        /// credentials flow. It throws an <see cref="AuthenticationException"/> if the token request fails.</remarks>
        /// <returns>A <see cref="TokenInfo"/> object containing the access token, refresh token, expiration time, and token
        /// type.</returns>
        /// <exception cref="AuthenticationException">Thrown if the token request results in an error. The exception message includes the status code and error
        /// details. This should trigger a logout in the global exception handler.</exception>
        private async Task<TokenInfo> ApiTokenAsync()
        {
            // No user: use client credentials
            HttpClient client = _httpClientFactory.CreateClient();
            TokenResponse tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = _authenticationServer + "/connect/token",
                ClientId = _authApiClientId,
                ClientSecret = _authenticationServerSecret,
                Scope = _scope
            });
            
            if (tokenResponse.IsError)
            {
                throw new AuthenticationException($"Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");
            }

            return new TokenInfo
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                TokenType = "Bearer"
            };
        }
    }
}
