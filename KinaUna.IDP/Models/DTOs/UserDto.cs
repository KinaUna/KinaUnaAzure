using System;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.IDP.Models.DTOs
{
    public class UserDto : IdentityUser
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public int ViewChild { get; set; }
        public string TimeZone { get; set; }
        public DateTime JoinDate { get; set; }
    }
}
