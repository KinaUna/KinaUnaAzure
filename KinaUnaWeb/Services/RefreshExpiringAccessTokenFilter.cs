using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace KinaUnaWeb.Services
{
    // Original Source: https://gist.github.com/devJ0n/43c6888161169e09fec542d2dc12af09

    public class RefreshExpiringAccessTokenFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;

        public RefreshExpiringAccessTokenFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var authorityServerUrl = _configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = _configuration.GetValue<string>("AuthenticationServerClientSecret");
            var identity = context.HttpContext.User.Identity as ClaimsIdentity;

            var oldExpiresAtClaim = identity.Claims.SingleOrDefault(claim => claim.Type == "expires_at1");

            string expiresAtString = oldExpiresAtClaim?.Value;
            var expiresAtLocal = DateTime.ParseExact(expiresAtString, "o", CultureInfo.InvariantCulture, DateTimeStyles.None);
            var expiresAtUtc = DateTime.ParseExact(expiresAtString, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            
            if (DateTimeOffset.UtcNow > expiresAtUtc)
            {
                var oldAccessTokenClaim = identity.Claims.SingleOrDefault(claim => claim.Type == OpenIdConnectParameterNames.AccessToken);
                var oldRefreshTokenClaim = identity.Claims.SingleOrDefault(claim => claim.Type == OpenIdConnectParameterNames.RefreshToken);
                
                // If we have to refresh, grab the refresh token from the claims, and request new access/refresh/id tokens
                var discoveryClient = new DiscoveryClient(authorityServerUrl);
                var doc = await discoveryClient.GetAsync();
                if (doc.IsError) throw new Exception(doc.Error);
                var tokenEndpoint = doc.TokenEndpoint;

                var tokenClient = new TokenClient(tokenEndpoint, authenticationServerClientId, authenticationServerClientSecret);
                var tokenResponse = await tokenClient.RequestRefreshTokenAsync(oldRefreshTokenClaim?.Value);

                if (!tokenResponse.IsError)
                {
                    var newIdToken = tokenResponse.IdentityToken;
                    var newAccessToken = tokenResponse.AccessToken;
                    var newRefreshToken = tokenResponse.RefreshToken;

                    //Remove old claims
                    identity.RemoveClaim(oldAccessTokenClaim);
                    identity.RemoveClaim(oldRefreshTokenClaim);
                    identity.RemoveClaim(oldExpiresAtClaim);

                    //Add new claims
                    identity.AddClaim(new Claim(OpenIdConnectParameterNames.AccessToken, newAccessToken));
                    identity.AddClaim(new Claim(OpenIdConnectParameterNames.RefreshToken, newRefreshToken));
                    
                    var expiresAt1 = DateTime.SpecifyKind(DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn), DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
                    identity.AddClaim(new Claim("expires_at1", expiresAt1));

                    var authResult = await context.HttpContext.AuthenticateAsync();
                    authResult.Properties.StoreTokens(new List<AuthenticationToken>
                    {
                        new AuthenticationToken { Name = OpenIdConnectParameterNames.IdToken, Value = newIdToken },
                        new AuthenticationToken { Name = OpenIdConnectParameterNames.AccessToken, Value = newAccessToken },
                        new AuthenticationToken { Name = OpenIdConnectParameterNames.RefreshToken, Value = newRefreshToken }
                    });

                    await context.HttpContext.SignOutAsync("Cookies");
                    await context.HttpContext.SignInAsync(new ClaimsPrincipal(identity), authResult.Properties);
                }
            }

            await next();
        }
    }
}
