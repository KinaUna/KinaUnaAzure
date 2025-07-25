using KinaUna.Data.Extensions;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Client.AspNetCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    // Reference: https://github.com/openiddict/openiddict-samples/blob/dev/samples/Velusia/Velusia.Client/Controllers/AuthenticationController.cs

    /// <summary>
    /// Provides authentication-related actions for logging in and out of the application.
    /// </summary>
    /// <remarks>This controller handles user authentication by integrating with an OpenID Connect provider.
    /// It includes actions for initiating login and logout processes, ensuring that return URLs are local to prevent
    /// open redirect attacks. The controller uses an <see cref="ITokenService"/> to manage user tokens during
    /// logout.</remarks>
    /// <param name="tokenService"></param>
    public class AuthenticationController(ITokenService tokenService) : Controller
    {
        /// <summary>
        /// Initiates the login process using OpenID Connect authentication.
        /// </summary>
        /// <remarks>If <paramref name="returnUrl"/> is not a local URL, the user will be redirected to
        /// the root URL ("/") after login.</remarks>
        /// <param name="returnUrl">The URL to redirect to after a successful login. Must be a local URL.</param>
        /// <returns>An <see cref="ActionResult"/> that challenges the user to authenticate.</returns>
        [AllowAnonymous]
        [HttpGet("~/login")]
        public ActionResult LogIn(string returnUrl)
        {
            AuthenticationProperties properties = new()
            {
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Logs out the current user by removing their authentication tokens and signing them out of the local session and redirecting to log out on the authentication server.
        /// </summary>
        /// <remarks>This method removes the user's cached access and refresh tokens and signs them out of
        /// the local authentication session. If the user is authenticated, it redirects them to the specified return
        /// URL or the home page if the URL is not local.</remarks>
        /// <param name="returnUrl">The URL to redirect to after logout. Must be a local URL to prevent open redirect attacks.</param>
        /// <returns>An <see cref="ActionResult"/> that redirects the user to the specified <paramref name="returnUrl"/> or the
        /// home page if the URL is not local.</returns>
        [AllowAnonymous]
        [HttpPost("~/logout"), ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOut(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(User.GetUserId()))
            {
                // Remove the user's cached access token and refresh token.
                await tokenService.RemoveTokenForUser(User.GetUserId());
            }
            // Retrieve the identity stored in the local authentication cookie. If it's not available,
            // this indicates that the user is already logged out locally (or has not logged in yet).
            //
            // For scenarios where the default authentication handler configured in the ASP.NET Core
            // authentication options shouldn't be used, a specific scheme can be specified here.
            AuthenticateResult result = await HttpContext.AuthenticateAsync();
            if (result is not { Succeeded: true })
            {
                // Only allow local return URLs to prevent open redirect attacks.
                return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
            }

            // Remove the local authentication cookie before triggering a redirection to the remote server.
            //
            // For scenarios where the default sign-out handler configured in the ASP.NET Core
            // authentication options shouldn't be used, a specific scheme can be specified here.
            await HttpContext.SignOutAsync();

            // If no properties were stored, redirect the user agent to the return URL.
            if (result.Properties == null) return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");

            AuthenticationProperties properties = new(new Dictionary<string, string>
            {
                // While not required, the specification encourages sending an id_token_hint
                // parameter containing an identity token returned by the server for this user.
                [OpenIddictClientAspNetCoreConstants.Properties.IdentityTokenHint] =
                    result.Properties.GetTokenValue(OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken)
            })
            {
                // Only allow local return URLs to prevent open redirect attacks.
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };

            // Ask the OpenIddict client middleware to redirect the user agent to the identity provider.
            return SignOut(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
