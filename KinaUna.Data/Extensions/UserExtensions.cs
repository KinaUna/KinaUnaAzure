using System.Security.Claims;
using IdentityModel;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extensions for User data.
    /// Note: The User object is the ClaimsPrincipal object from the HttpContext, the data is provided by IdentityServer and doesn't automatically reflect a user's UserInfo data.
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

        /// <summary>
        /// Obtains the user's timezone from the ClaimsPrincipal.
        /// Note: This is the timezone set for IdentityServer, not the timezone set in UserInfo.
        /// </summary>
        /// <param name="principal"></param>
        /// <returns>string: The timezone</returns>
        public static string GetUserTimeZone(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type.Equals("timezone" ))?.Value;
        }

        /// <summary>
        /// Obtains the user's username from the ClaimsPrincipal.
        /// Note: This is the username for the IdentityServer user account, not the username set in UserInfo.
        /// </summary>
        /// <param name="principal"></param>
        /// <returns>string: The username.</returns>
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
