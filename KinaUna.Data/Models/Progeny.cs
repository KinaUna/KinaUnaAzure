using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Progeny data.
    /// </summary>
    public class Progeny
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(256)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string NickName { get; set; }
        public DateTime? BirthDay { get; set; }

        [MaxLength(256)]
        public string TimeZone { get; set; }

        [MaxLength(512)]
        public string PictureLink { get; set; }

        [MaxLength(1024)]
        public string Admins { get; set; } // Comma separated list of emails.

        public string Email { get; set; } // KinaUna account email
        public string UserId { get; set; } // KinaUna account UserId

        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

    }
}
