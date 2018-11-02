using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.IDP.Models
{
    public class ApplicationUser : IdentityUser
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
