using System.Security.Claims;
using System.Security.Principal;
using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public class IdentityParser : IIdentityParser<ApplicationUser>
    {
        public ApplicationUser Parse(IPrincipal principal)
        {
            // Pattern matching 'is' expression
            // assigns "claims" if "principal" is a "ClaimsPrincipal"
            if (principal is not ClaimsPrincipal claims) throw new ArgumentException(message: "The principal must be a ClaimsPrincipal", paramName: nameof(principal));

            bool joinDateParse =
                DateTime.TryParse(claims.Claims.FirstOrDefault(x => x.Type == "joindate")?.Value ?? "",
                    out DateTime joinDateValue);
            if (!joinDateParse)
            {
                joinDateValue = DateTime.Now;
            }
            return new ApplicationUser
            {

                FirstName = claims.Claims.FirstOrDefault(x => x.Type == "firstname")?.Value ?? "",
                MiddleName = claims.Claims.FirstOrDefault(x => x.Type == "middlename")?.Value ?? "",
                LastName = claims.Claims.FirstOrDefault(x => x.Type == "lastname")?.Value ?? "",
                ViewChild = int.Parse(claims.Claims.FirstOrDefault(x => x.Type == "viewchild")?.Value ?? "0"),
                TimeZone = claims.Claims.FirstOrDefault(x => x.Type == "timezone")?.Value ?? "",
                JoinDate = joinDateValue,
                Email = claims.Claims.FirstOrDefault(x => x.Type == "email")?.Value ?? "",
                Id = claims.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? "",
                UserName = claims.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value ?? "",
                PhoneNumber = claims.Claims.FirstOrDefault(x => x.Type == "phone_number")?.Value ?? "",
                    
            };
        }
    }
}
