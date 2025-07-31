using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Validation;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace KinaUnaProgenyApi.AuthorizationHandlers
{
    public interface ITokenValidationService
    {
        Task<ClaimsPrincipal> ValidateTokenAsync(string token);
    }

    /// <summary>
    /// Provides a token validation service that caches validation results to improve performance and reduce redundant
    /// validation operations.
    /// </summary>
    /// <remarks>This service wraps an underlying <see cref="OpenIddictValidationService"/> to validate tokens
    /// and caches the results using an <see cref="IMemoryCache"/>. Cached results are stored with both absolute and
    /// sliding expiration policies to balance performance and token validity.</remarks>
    /// <param name="validationService"></param>
    /// <param name="cache"></param>
    /// <param name="logger"></param>
    public class CachedTokenValidationService(OpenIddictValidationService validationService, IMemoryCache cache, ILogger<CachedTokenValidationService> logger)
        : ITokenValidationService
    {
        /// <summary>
        /// Validates the specified token and returns the associated claims principal if the token is valid.
        /// </summary>
        /// <remarks>This method uses caching to optimize performance by storing the validation result for
        /// a token.  If the token has been validated previously and is still within the cache duration, the cached 
        /// result is returned. Otherwise, the token is validated using the underlying validation service. <para> The
        /// cache duration is determined by the absolute and sliding expiration settings defined in  <see
        /// cref="AuthConstants.ProgenyApiTokenCacheDurationMinutes"/> and  <see
        /// cref="AuthConstants.ProgenyApiTokenCacheSlidingExpirationMinutes"/>. </para> <para> If validation fails, the
        /// token is removed from the cache, and an error is logged. </para></remarks>
        /// <param name="token">The token to validate. Cannot be null or empty.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> representing the validated token's claims if the token is valid;  otherwise,
        /// <see langword="null"/> if the token is invalid or validation fails.</returns>
        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
        {
            string cacheKey = $"token_{ComputeHash(token)}";

            if (cache.TryGetValue(cacheKey, out ClaimsPrincipal cachedPrincipal))
            {
                logger.LogDebug("Token validation result retrieved from cache");
                return cachedPrincipal;
            }

            try
            {
                // Use OpenIddict's ValidateAccessTokenAsync method
                ClaimsPrincipal principal = await validationService.ValidateAccessTokenAsync(token);

                if (principal.GetClaim(OpenIddictConstants.Claims.ClientId) == null) return null;
                MemoryCacheEntryOptions cacheOptions = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(AuthConstants.ProgenyApiTokenCacheDurationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(AuthConstants.ProgenyApiTokenCacheSlidingExpirationMinutes)
                };

                cache.Set(cacheKey, principal, cacheOptions);
                logger.LogDebug("Token validation result cached");

                return principal;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token validation failed");
                cache.Remove(cacheKey); // Remove from cache if validation fails
                return null;
            }
        }

        /// <summary>
        /// Computes the SHA-256 hash of the specified input string and returns it as a Base64-encoded string.
        /// </summary>
        /// <param name="input">The input string to compute the hash for. Cannot be <see langword="null"/> or empty.</param>
        /// <returns>A Base64-encoded string representing the SHA-256 hash of the input.</returns>
        private static string ComputeHash(string input)
        {
            byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }
    }
}
