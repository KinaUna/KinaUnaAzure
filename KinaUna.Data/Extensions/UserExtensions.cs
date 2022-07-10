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
            string userEmailString = principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.Email))?.Value;
            if (string.IsNullOrEmpty(userEmailString))
            {
                userEmailString = Constants.DefaultUserEmail;
            }

            return userEmailString;
        }

        /// <summary>
        /// Gets the users User Id.
        /// </summary>
        /// <param name="principal">ClaimsPrincipal: The User object to obtain an Id from.</param>
        /// <returns>string: The User's User Id.</returns>
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            string userIdString = principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.Subject))?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                userIdString = Constants.DefaultUserId;
            }

            return userIdString;
        }

        public static string GetUserTimeZone(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type.Equals("timezone" ))?.Value;
        }

        public static string GetUserUserName(this ClaimsPrincipal principal)
        {
            string userNameString = principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.PreferredUserName))?.Value;
            if (string.IsNullOrEmpty(userNameString))
            {
                userNameString = "Unknown user name";
            }

            return userNameString;
        }
    }
}
