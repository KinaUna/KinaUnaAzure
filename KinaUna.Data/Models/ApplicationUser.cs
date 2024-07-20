using System;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for user data for IdentityServer.
    /// </summary>
    public class ApplicationUser: IdentityUser
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public int ViewChild { get; set; }
        public string TimeZone { get; set; }
        public DateTime JoinDate { get; set; }
        public string Role { get; set; }
    }
}
