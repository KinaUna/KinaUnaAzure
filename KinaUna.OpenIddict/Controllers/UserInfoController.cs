using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KinaUna.OpenIddict.Controllers
{
    public class UserinfoController(UserManager<ApplicationUser> userManager) : Controller
    {
        //
        // GET: /api/userinfo
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo"), HttpPost("~/connect/userinfo"), Produces("application/json")]
        public async Task<IActionResult> Userinfo()
        {
            ApplicationUser? user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified access token is bound to an account that no longer exists."
                    }));
            }

            Dictionary<string, object> claims = new(StringComparer.Ordinal)
            {
                // Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
                [Claims.Subject] = await userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(Scopes.Email))
            {
                string? userEmail = await userManager.GetEmailAsync(user);
                claims[Claims.Email] = userEmail ?? string.Empty;
                claims[Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user);
            }
            
            if (User.HasScope(Scopes.Roles))
            {
                claims[Claims.Role] = await userManager.GetRolesAsync(user);
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.PreferredUsername] = user.UserName ?? string.Empty;
            }

            // Note: the complete list of standard claims supported by the OpenID Connect specification
            // can be found here: http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims

            return Ok(claims);
        }
    }
}
