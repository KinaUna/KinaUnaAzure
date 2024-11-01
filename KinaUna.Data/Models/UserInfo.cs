using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for UserInfo data.
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string UserId { get; set; }

        [MaxLength(256)]
        public string UserEmail { get; set; } = Constants.DefaultUserEmail;

        [MaxLength(256)]
        public string UserName { get; set; }

        [MaxLength(256)]
        public string FirstName { get; set; }

        [MaxLength(256)]
        public string MiddleName { get; set; }

        [MaxLength(256)]
        public string LastName { get; set; }

        [MaxLength(256)]
        public string PhoneNumber { get; set; }

        public int ViewChild { get; set; }

        [MaxLength(256)]
        public string Timezone { get; set; }

        [MaxLength(1024)]
        public string ProfilePicture { get; set; }
        public bool IsKinaUnaAdmin { get; set; }
        public DateTime UpdatedTime { get; set; }
        public bool Deleted { get; set; }
        public DateTime DeletedTime { get; set; }

        [NotMapped]
        public List<Progeny> ProgenyList { get; set; }
        [NotMapped]
        public bool CanUserAddItems { get; set; }
        [NotMapped]
        public List<UserAccess> AccessList { get; set; }
        [NotMapped]
        public bool UpdateIsAdmin { get; set; }
    }
}
