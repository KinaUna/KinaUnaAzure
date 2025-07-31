using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.AuthorizationHandlers
{
    /// <summary>
    /// Provides an authorization handler that validates whether a user or client meets the requirements specified by
    /// the <see cref="UserOrClientRequirement"/>.
    /// </summary>
    /// <remarks>This handler checks if the current user is authenticated and has valid claims, or if the
    /// client making the request is authorized based on its client ID. It supports token validation using a caching
    /// mechanism to improve performance. If either the user or the client meets the specified requirements, the
    /// authorization succeeds.</remarks>
    /// <param name="tokenValidationService"></param>
    /// <param name="logger"></param>
    public class UserOrClientHandler(
        ITokenValidationService tokenValidationService,
        ILogger<UserOrClientHandler> logger) : AuthorizationHandler<UserOrClientRequirement>
    {
        /// <summary>
        /// Handles the authorization requirement by validating the user's or client's claims.
        /// </summary>
        /// <remarks>This method checks whether the user or client satisfies the specified authorization
        /// requirement.  If the user is authenticated, it attempts to validate the user's token using a cached
        /// validation mechanism.  If validation succeeds, the method evaluates the presence of specific claims, such as
        /// "sub" for users  or "client_id" for clients, to determine whether the requirement is met.  The method
        /// succeeds the requirement if either a valid user or client is identified.</remarks>
        /// <param name="context">The authorization context that contains information about the current authorization request.</param>
        /// <param name="requirement">The requirement to evaluate for the current authorization request.</param>
        /// <returns></returns>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserOrClientRequirement requirement)
        {
            // If user is not authenticated, try cached validation first
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                if(await TryValidateWithCache(context))
                {
                    logger.LogDebug("User authenticated using cached token validation");
                    // Now proceed with your existing logic
                    bool hasUser = context.User.HasClaim(c => c.Type == "sub") && (context.User.Identity?.IsAuthenticated ?? false);
                    bool hasClient = context.User.HasClaim(c => c.Type == "client_id");

                    // Check client_id value if it exists
                    if (hasClient)
                    {
                        string clientId = context.User.FindFirst(c => c.Type == "client_id")?.Value;
                        if (string.IsNullOrEmpty(clientId))
                        {
                            hasClient = false;
                        }
                        // Check if the client_id is one of the allowed values
                        else if (clientId != "kinaunawebclient" &&
                                 clientId != "kinaunawebclientlocal" &&
                                 clientId != "kinaunawebclientazure")
                        {
                            hasClient = false;
                        }
                    }

                    if (hasUser || hasClient)
                    {
                        context.Succeed(requirement);
                    }
                }
                else
                {
                    logger.LogDebug("User not authenticated, no valid token found in cache");
                }
            }
        }

        private async Task<bool> TryValidateWithCache(AuthorizationHandlerContext context)
        {
            if (context.Resource is not HttpContext httpContext) return false;
            string token = ExtractToken(httpContext);
            if (string.IsNullOrEmpty(token)) return false;
            try
            {
                ClaimsPrincipal principal = await tokenValidationService.ValidateTokenAsync(token);
                if (principal != null)
                {
                    logger.LogDebug("Token validated using cached service");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cached token validation failed, falling back to standard validation");
            }

            return false;
        }

        /// <summary>
        /// Extracts the Bearer token from the Authorization header of the HTTP request.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> containing the HTTP request with the Authorization header.</param>
        /// <returns>The extracted Bearer token as a string, or <see langword="null"/> if the Authorization header is not
        /// present, does not start with "Bearer ", or is otherwise invalid.</returns>
        private static string ExtractToken(HttpContext context)
        {
            string authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            return authHeader?.StartsWith("Bearer ") == true
                ? authHeader["Bearer ".Length..].Trim()
                : null;
        }
    }
}
