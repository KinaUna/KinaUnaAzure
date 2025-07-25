using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Client.AspNetCore;

namespace KinaUna.OpenIddict.Controllers
{
    // Based on: https://github.com/openiddict/openiddict-samples/blob/dev/samples/Velusia/Velusia.Server/Controllers/AuthenticationController.cs

    public class AuthenticationController : Controller
    {
        /// <summary>
        /// Initiates the login process using OpenID Connect authentication.
        /// </summary>
        /// <remarks>This method is accessible without authentication and redirects the user to the
        /// specified local URL upon successful login. If the <paramref name="returnUrl"/> is not a local URL, the user
        /// is redirected to the root URL ("/").</remarks>
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
        /// Logs out the current user and redirects them to the specified return URL.
        /// </summary>
        /// <remarks>This method removes the local authentication cookie and, if applicable, redirects the
        /// user agent to the identity provider using the OpenIddict client middleware. It ensures that only local URLs
        /// are used for redirection to prevent open redirect attacks.</remarks>
        /// <param name="returnUrl">The URL to redirect to after logging out. Must be a local URL to prevent open redirect attacks.</param>
        /// <returns>An <see cref="ActionResult"/> that redirects the user to the specified return URL or the root URL if the
        /// return URL is not local.</returns>
        [AllowAnonymous]
        [HttpPost("~/logout"), ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOut(string returnUrl)
        {
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
            string tokenHint = result.Properties.GetTokenValue(OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken) ?? string.Empty;
            AuthenticationProperties properties = new(new Dictionary<string, string?>
            {
                // While not required, the specification encourages sending an id_token_hint
                // parameter containing an identity token returned by the server for this user.
                [OpenIddictClientAspNetCoreConstants.Properties.IdentityTokenHint] = tokenHint
                    
            })
            {
                // Only allow local return URLs to prevent open redirect attacks.
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };

            // Ask the OpenIddict client middleware to redirect the user agent to the identity provider.
            return SignOut(properties, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
