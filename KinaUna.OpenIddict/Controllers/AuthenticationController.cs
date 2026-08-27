using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
            return Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
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
            if (Url.IsLocalUrl(returnUrl))
            {
                await HttpContext.SignOutAsync(); // uses the default cookie scheme
                return Redirect(returnUrl);
            }

            return Redirect("/");
        }
    }
}
