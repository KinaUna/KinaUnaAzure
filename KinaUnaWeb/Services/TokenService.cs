using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace KinaUnaWeb.Services
{
    public class TokenInfo
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
    }

    public interface ITokenService
    {
        Task<TokenInfo> GetValidTokenAsync(string userId);
        Task StoreTokenAsync(string userId, TokenInfo token);
        Task RemoveTokenForUser(string userId);
    }

    /// <summary>
    /// Provides functionality for managing and retrieving authentication tokens for API access.
    /// </summary>
    /// <remarks>The <see cref="TokenService"/> class is responsible for obtaining, refreshing, and storing API
    /// authentication tokens for users. It supports token retrieval for both authenticated users and API access when no
    /// user is specified. The service uses an <see cref="IHttpClientFactory"/> to make HTTP requests to the
    /// authentication server and an <see cref="IHttpContextAccessor"/> to access the current HTTP context for token
    /// management.</remarks>
    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _authenticationServer;
        private readonly string _webClientId;
        private readonly string _webApiClientId;
        private readonly string _authenticationServerSecret;
        private readonly string _scope;

        // Todo: This won't scale well for many users, and isn't persisted when restarting the server, consider using a distributed cache or database.
        private readonly ConcurrentDictionary<string, TokenInfo> _accessTokens = new();

        public TokenService(IHttpClientFactory httpClientFactory, IHostEnvironment env, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;

            string scope = OpenIddictConstants.Scopes.OfflineAccess + " " +
                           OpenIddictConstants.Scopes.Email + " " +
                           OpenIddictConstants.Scopes.Profile + " " +
                           OpenIddictConstants.Scopes.Roles;


            if (env.IsDevelopment())
            {
                _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Local");
                _webClientId = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Local");
                _webApiClientId = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Local");
                _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Local");
                _scope = scope + " " + AuthConstants.ProgenyApiName + "local " + AuthConstants.AuthApiName + "local";
            }
            else
            {
                if (env.IsStaging())
                {
                    _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey + "Azure");
                    _webClientId = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey + "Azure");
                    _webApiClientId = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey + "Azure");
                    _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey + "Azure");
                    _scope = scope + " " + AuthConstants.ProgenyApiName + "azure " + AuthConstants.AuthApiName + "azure";
                }
                else
                {
                    _authenticationServer = configuration.GetValue<string>(AuthConstants.AuthenticationServerUrlKey);
                    _webClientId = configuration.GetValue<string>(AuthConstants.WebServerClientIdKey);
                    _webApiClientId = configuration.GetValue<string>(AuthConstants.WebServerApiClientIdKey);
                    _authenticationServerSecret = configuration.GetValue<string>(AuthConstants.WebServerClientSecretKey);
                    _scope = scope + " " + AuthConstants.ProgenyApiName + " " + AuthConstants.AuthApiName;
                }
            }
            
        }

        /// <summary>
        /// Asynchronously retrieves a valid token for the specified user.
        /// </summary>
        /// <remarks>This method attempts to retrieve a valid token from the cache. If the token is not
        /// found or is expired, it tries to refresh or exchange the token using available credentials.</remarks>
        /// <param name="userId">The identifier of the user for whom the token is requested. If null or empty, a token for API access is
        /// returned.</param>
        /// <returns>A <see cref="TokenInfo"/> object representing the valid token for the user.</returns>
        /// <exception cref="AuthenticationException">Thrown if the user is not authenticated, the HttpContext is null, or no valid token can be retrieved or
        /// refreshed. This should trigger a logout in the global Exception handler.</exception>
        public async Task<TokenInfo> GetValidTokenAsync(string userId)
        {
            // Check if the userId is null or empty, and return a token for API access
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = AuthConstants.ProgenyApiName; // Use a constant for API access
            }

            TokenInfo apiTokenInfo = null;
            if (_accessTokens.TryGetValue(userId, out TokenInfo cachedTokenInfo))
            {
                if (cachedTokenInfo != null && DateTime.UtcNow < cachedTokenInfo.AccessTokenExpiresAt.AddMinutes(-1))
                    return cachedTokenInfo;

                if (userId != AuthConstants.ProgenyApiName)
                {
                    // If the token is expired or about to expire, refresh it
                    if (cachedTokenInfo != null && !string.IsNullOrWhiteSpace(cachedTokenInfo.RefreshToken))
                    {
                        try
                        {
                            TokenInfo refreshApiTokenInfo = await RefreshTokenAsync(cachedTokenInfo.RefreshToken);
                            apiTokenInfo = await ExchangeTokenAsync(refreshApiTokenInfo.AccessToken);
                            if (apiTokenInfo != null)
                            {
                                // Update the token in the cache
                                await UpdateTokenAsync(userId, apiTokenInfo);
                                return apiTokenInfo;
                            }
                        }
                        catch (AuthenticationException)
                        {
                            // If refresh fails, remove the token and continue to get a new one
                            await RemoveTokenForUser(userId);
                        }
                    }
                }
            }

            // If userId was null or empty, return a token for API access
            if (userId == AuthConstants.ProgenyApiName)
            {
                apiTokenInfo = await ApiTokenAsync();
            }

            // If no valid token found, try to exchange the access token
            if (apiTokenInfo == null)
            {
                if (_httpContextAccessor.HttpContext == null)
                {
                    throw new AuthenticationException("HttpContext is null. Cannot retrieve token.");
                }
                if (_httpContextAccessor.HttpContext.User.Identity == null || !_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                {
                    throw new AuthenticationException("User is not authenticated. Cannot retrieve token.");
                }

                // Try to get the access token from the current HttpContext
                string accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    // If the access token is available, exchange it for a new api token
                    apiTokenInfo = await ExchangeTokenAsync(accessToken);
                }
            }
            
            if (apiTokenInfo == null)
            {
                throw new AuthenticationException("No valid token found or could not refresh the token.");
            }

            await StoreTokenAsync(userId, apiTokenInfo);
            return apiTokenInfo;
        }

        /// <summary>
        /// Asynchronously stores the specified token information for a given user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user for whom the token is being stored. Cannot be null or empty.</param>
        /// <param name="token">The token information to store. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task StoreTokenAsync(string userId, TokenInfo token)
        {
            _accessTokens.TryAdd(userId, token);
            return Task.CompletedTask;
        }

        private Task UpdateTokenAsync(string userId, TokenInfo token)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null.");
            }
            _accessTokens.AddOrUpdate(userId, token, (_, _) => token);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the access token associated with the specified user.
        /// </summary>
        /// <remarks>This method checks if an access token exists for the specified user and removes it if
        /// present.  It is a no-op if no token is associated with the user.</remarks>
        /// <param name="userId">The unique identifier of the user whose access token is to be removed. Cannot be null or empty.</param>
        /// <returns></returns>
        public Task RemoveTokenForUser(string userId)
        {
            var tokensToRemove = _accessTokens.ContainsKey(userId);
            if (tokensToRemove)
            {
                _accessTokens.TryRemove(userId, out _);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes the access token using the provided refresh token.
        /// </summary>
        /// <remarks>This method communicates with the authentication server to request a new access token
        /// using the provided refresh token. It updates the authentication properties with the new tokens and signs in
        /// the user with the updated properties if the HTTP context is available.</remarks>
        /// <param name="refreshToken">The refresh token used to obtain a new access token. Cannot be null or empty.</param>
        /// <returns>A <see cref="TokenInfo"/> object containing the new access token, refresh token, and expiration time.</returns>
        /// <exception cref="AuthenticationException">Thrown if the refresh token response contains an error, or if the response does not include a valid access
        /// token, refresh token, or expiration time. This should trigger a logout in the global exception handler.</exception>
        private async Task<TokenInfo> RefreshTokenAsync(string refreshToken)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            TokenResponse refreshTokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = _authenticationServer + "/connect/token",
                ClientId = _webClientId,
                ClientSecret = _authenticationServerSecret,
                RefreshToken = refreshToken
            });
            if (refreshTokenResponse.IsError)
            {
                throw new AuthenticationException($"Status code: {refreshTokenResponse.IsError}, Error: {refreshTokenResponse.Error}");
            }

            // Set the new access token and refresh token
            if (string.IsNullOrWhiteSpace(refreshTokenResponse.AccessToken))
            {
                throw new AuthenticationException("Refresh token response does not contain an access token.");
            }
            if (string.IsNullOrWhiteSpace(refreshTokenResponse.RefreshToken))
            {
                throw new AuthenticationException("Refresh token response does not contain a refresh token.");
            }
            if (refreshTokenResponse.ExpiresIn <= 0)
            {
                throw new AuthenticationException("Refresh token response does not contain a valid expiration time.");
            }

            return new TokenInfo
            {
                AccessToken = refreshTokenResponse.AccessToken,
                RefreshToken = refreshTokenResponse.RefreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshTokenResponse.ExpiresIn),
                TokenType = "Bearer"
            };
        }

        /// <summary>
        /// Exchanges a subject token for a new access token using the token exchange protocol.
        /// </summary>
        /// <remarks>This method uses the OAuth 2.0 token exchange protocol to obtain a new access token
        /// from the authentication server. Ensure that the provided <paramref name="subjectToken"/> is valid and that
        /// the client credentials are correctly configured.</remarks>
        /// <param name="subjectToken">The subject token to be exchanged. This token must be a valid access token.</param>
        /// <returns>A <see cref="TokenInfo"/> object containing the new access token, refresh token, and expiration details.</returns>
        /// <exception cref="AuthenticationException">Thrown if the token exchange process fails, indicating an error with the authentication server response.
        /// This should trigger a logout in the global exception handler.</exception>
        private async Task<TokenInfo> ExchangeTokenAsync(string subjectToken)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            TokenResponse tokenExchangeResponse = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
            {
                Address = _authenticationServer + "/connect/token",
                ClientId = _webClientId,
                ClientSecret = _authenticationServerSecret,
                SubjectToken = subjectToken,
                SubjectTokenType = "urn:ietf:params:oauth:token-type:access_token",
                Scope = _scope,
            });

            if (tokenExchangeResponse.IsError)
            {
                throw new AuthenticationException($"Status code: {tokenExchangeResponse.IsError}, Error: {tokenExchangeResponse.Error}");
            }

            return new TokenInfo
            {
                AccessToken = tokenExchangeResponse.AccessToken,
                RefreshToken = tokenExchangeResponse.RefreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenExchangeResponse.ExpiresIn),
                TokenType = "Bearer"
            };
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
                ClientId = _webApiClientId,
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
