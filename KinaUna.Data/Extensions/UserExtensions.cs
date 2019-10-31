using System.Security.Claims;
using IdentityModel;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extensions for User data.
    /// </summary>
    public static class UserExtensions
    {
        /// <summary>
        /// Gets the users email address.
        /// </summary>
        /// <param name="principal">ClaimsPrincipal: The User object to obtain an email address from.</param>
        /// <returns>string: The User's email address.</returns>
        public static string GetEmail(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.Email))?.Value;
        }

        /// <summary>
        /// Gets the users User Id.
        /// </summary>
        /// <param name="principal">ClaimsPrincipal: The User object to obtain an Id from.</param>
        /// <returns>string: The User's User Id.</returns>
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.Subject))?.Value;
        }
    }
}
