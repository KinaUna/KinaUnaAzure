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
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));
            var subjectIdClaim = subject?.Claims.FirstOrDefault(x => x.Type == "sub");
            if (subjectIdClaim != null)
            {
                var subjectId = subjectIdClaim.Value;
                var user = await _userManager.FindByIdAsync(subjectId);
                if (user == null)
                    throw new ArgumentException("Invalid subject identifier");

                var claims = GetClaimsFromUser(user);
                context.IssuedClaims = claims.ToList();
            }

        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var subject = context.Subject ?? throw new ArgumentNullException(nameof(context.Subject));

            var subjectIdClaim = subject?.Claims.FirstOrDefault(x => x.Type == "sub");
            if (subjectIdClaim != null)
            {
                var subjectId = subjectIdClaim.Value;
                var user = await _userManager.FindByIdAsync(subjectId);

                context.IsActive = false;

                if (user != null)
                {
                    if (_userManager.SupportsUserSecurityStamp)
                    {
                        var securityStamp = subject.Claims.Where(c => c.Type == "security_stamp").Select(c => c.Value).SingleOrDefault();
                        if (securityStamp != null)
                        {
                            var dbSecurityStamp = await _userManager.GetSecurityStampAsync(user);
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

        private IEnumerable<Claim> GetClaimsFromUser(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id),
                new Claim(JwtClaimTypes.PreferredUserName, user.UserName),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            };

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

            if (_userManager.SupportsUserEmail)
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Email, user.Email),
                    new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            if (_userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean)
                });
            }

            return claims;
        }
    }
}
