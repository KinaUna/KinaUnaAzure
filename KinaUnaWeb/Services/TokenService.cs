using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public class KinaunaTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }   // only for auth code flow
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<KinaunaTokenResponse> RedeemAuthorizationCodeAsync(string code, string redirectUri)
        {
            HttpClient client = _httpClientFactory.CreateClient("oidc");

            var session = _httpContextAccessor.HttpContext?.Session;
            string codeVerifier = session?.GetString("code_verifier")!;

            HttpRequestMessage req = new(HttpMethod.Post, "/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["client_id"] = _configuration["Auth:ClientId_WebUi"],
                    ["client_secret"] = _configuration["Auth:ClientSecret_WebUi"],
                    // if you used PKCE:
                    ["code_verifier"] = codeVerifier,
                })
            };

            HttpResponseMessage resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            JsonElement? payloadNullable = await resp.Content.ReadFromJsonAsync<JsonElement?>();
            if (payloadNullable == null)
            {
                throw new InvalidOperationException("Invalid token response");
            }
            JsonElement payload = payloadNullable.Value;

            return new KinaunaTokenResponse
            {
                AccessToken = payload.GetProperty("access_token").GetString()!,
                RefreshToken = payload.TryGetProperty("refresh_token", out var rt) ? rt.GetString()! : null,
                ExpiresIn = payload.GetProperty("expires_in").GetInt32(),
                TokenType = payload.GetProperty("token_type").GetString()!,
            };
        }

        public async Task<KinaunaTokenResponse> GetClientCredentialsTokenAsync()
        {
            HttpClient client = _httpClientFactory.CreateClient("oidc");

            HttpRequestMessage req = new(HttpMethod.Post, "/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "api.read",  // adjust as needed
                    ["client_id"] = _configuration["Auth:ClientId_ApiClient"],
                    ["client_secret"] = _configuration["Auth:ClientSecret_ApiClient"],
                })
            };

            HttpResponseMessage resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            JsonElement? payloadNullable = await resp.Content.ReadFromJsonAsync<JsonElement?>();
            if (payloadNullable == null)
            {
                throw new InvalidOperationException("Invalid token response");
            }

            JsonElement payload = payloadNullable.Value;

            return new KinaunaTokenResponse
            {
                AccessToken = payload.GetProperty("access_token").GetString()!,
                ExpiresIn = payload.GetProperty("expires_in").GetInt32(),
                TokenType = payload.GetProperty("token_type").GetString()!,
            };
        }
    }
}
