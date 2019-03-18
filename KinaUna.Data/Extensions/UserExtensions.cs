using System.Security.Claims;
using IdentityModel;

namespace KinaUna.Data.Extensions
{
    public static class UserExtensions
    {
        public static string GetEmail(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type.Equals(JwtClaimTypes.Email))?.Value;
        }
    }
}
