using System.Security.Principal;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Parsing methods for converting ClaimsPrincipal (usually HttpContext.User) to other types.
    /// </summary>
    public interface IIdentityParser<T>
    {
        /// <summary>
        /// Parses a ClaimsPrincipal (usually HttpContext.User) to an ApplicationUser object.
        /// </summary>
        /// <param name="principal">The ClaimsPrincipal (i.e. User) to parse.</param>
        /// <returns>ApplicationUser object.</returns>
        T Parse(IPrincipal principal);
    }
}
