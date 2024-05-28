using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using KinaUna.Data.Models;

namespace KinaUna.IDP.Services
{
    public class ProfileService(UserManager<ApplicationUser> userManager) : IProfileService
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            ClaimsPrincipal subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));
            Claim subjectIdClaim = subject.Claims.FirstOrDefault(x => x.Type == "sub");
            if (subjectIdClaim != null)
            {
                string subjectId = subjectIdClaim.Value;
                ApplicationUser user = await userManager.FindByIdAsync(subjectId) ?? throw new ArgumentException("Invalid subject identifier");
                IEnumerable<Claim> claims = GetClaimsFromUser(user);
                context.IssuedClaims = claims.ToList();
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
        public async Task IsActiveAsync(IsActiveContext context)
        {
            ClaimsPrincipal subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            Claim subjectIdClaim = subject.Claims.FirstOrDefault(x => x.Type == "sub");
            if (subjectIdClaim != null)
            {
                string subjectId = subjectIdClaim.Value;
                ApplicationUser user = await userManager.FindByIdAsync(subjectId);

                context.IsActive = false;

                if (user != null)
                {
                    if (userManager.SupportsUserSecurityStamp)
                    {
                        string securityStamp = subject.Claims.Where(c => c.Type == "security_stamp").Select(c => c.Value).SingleOrDefault();
                        if (securityStamp != null)
                        {
                            string dbSecurityStamp = await userManager.GetSecurityStampAsync(user);
                            if (dbSecurityStamp != securityStamp)
                                return;
                        }
                    }

                    context.IsActive =
                        !user.LockoutEnabled ||
                        !user.LockoutEnd.HasValue ||
                        user.LockoutEnd <= DateTime.Now;
                }
            }

        }

        private List<Claim> GetClaimsFromUser(ApplicationUser user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Email))
            {
                return [];
            }

            List<Claim> claims =
            [
                new Claim(JwtClaimTypes.Subject, user.Id),
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            ];

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                claims.Add(new Claim("firstname", user.FirstName));

            if (!string.IsNullOrWhiteSpace(user.MiddleName))
                claims.Add(new Claim("middlename", user.MiddleName));

            if (!string.IsNullOrWhiteSpace(user.LastName))
                claims.Add(new Claim("lastname", user.LastName));

            if (user.ViewChild > 0)
                claims.Add(new Claim("viewchild", user.ViewChild.ToString()));

            if (!string.IsNullOrWhiteSpace(user.TimeZone))
            {
                claims.Add(new Claim("timezone", user.TimeZone));
            }
            else
            {
                claims.Add(new Claim("timezone", "Central European Standard Time"));
            }


            claims.Add(new Claim("joindate", user.JoinDate.ToString("G")));
            if (!string.IsNullOrWhiteSpace(user.Role))
            {
                claims.Add(new Claim("role", user.Role));
            }

            if (userManager.SupportsUserEmail)
            {
                claims.AddRange(
                [
                    new Claim(JwtClaimTypes.Email, user.Email),
                    new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                ]);
            }

            if (userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                claims.AddRange(
                [
                    new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                ]);
            }

            return claims;
        }
    }
}
