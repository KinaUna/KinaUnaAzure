using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Progeny data.
    /// </summary>
    public class Progeny
    {
        /// <summary>
        /// Auto-incremented primary key for the Progeny entity.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The name of the progeny.
        /// </summary>
        [MaxLength(256)]
        public string Name { get; set; }

        /// <summary>
        /// The nickname of the progeny. This is used when displaying the name of the progeny in the UI.
        /// </summary>
        [MaxLength(256)]
        public string NickName { get; set; }

        /// <summary>
        /// The date and time of birth of the progeny.
        /// </summary>
        public DateTime? BirthDay { get; set; }

        /// <summary>
        /// The timezone of the Birthday. This is used to calculate the age of the progeny correctly.
        /// </summary>
        [MaxLength(256)]
        public string TimeZone { get; set; }

        /// <summary>
        /// The filename of the picture of the progeny. This is used to display the picture of the progeny in the UI.
        /// </summary>
        [MaxLength(512)]
        public string PictureLink { get; set; }

        /// <summary>
        /// Comma separated list of emails of the administrators for the progeny.
        /// </summary>
        [MaxLength(1024)]
        public string Admins { get; set; } // Comma separated list of emails.

        /// <summary>
        /// The email associated with the KinaUna account for the progeny.
        /// </summary>
        [MaxLength(1024)]
        public string Email { get; set; } // KinaUna account email
        
        /// <summary>
        /// Gets or sets the unique identifier for the user associated with the KinaUna account.
        /// </summary>
        [MaxLength(256)]
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

        [NotMapped] 
        public ProgenyPermission ProgenyPerMission { get; set; } = new();

    }
}
