using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for user data for IdentityServer.
    /// </summary>
    public class ApplicationUser: IdentityUser
    {
        /// <summary>
        /// First name of the user.
        /// </summary>
        [MaxLength(256)]
        public string FirstName { get; set; }
        /// <summary>
        /// Middle name of the user.
        /// </summary>
        [MaxLength(256)]
        public string MiddleName { get; set; }
        /// <summary>
        /// Last name of the user.
        /// </summary>
        [MaxLength(256)]
        public string LastName { get; set; }
        /// <summary>
        /// Default Progeny Id for the user.
        /// Obsolete. Use UserInfo from ProgenyApi instead.
        /// </summary>
        public int ViewChild { get; set; }
        /// <summary>
        /// User's time zone.
        /// Only used during signup, then stored in UserInfo in ProgenyApi.
        /// </summary>
        [MaxLength(256)]
        public string TimeZone { get; set; }
        /// <summary>
        /// The date the user signed up.
        /// </summary>
        public DateTime JoinDate { get; set; }
        /// <summary>
        /// User role. Not used.
        /// </summary>
        [MaxLength(128)]
        public string Role { get; set; }
    }
}
