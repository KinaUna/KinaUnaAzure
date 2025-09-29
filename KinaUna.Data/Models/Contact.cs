using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Contact data.
    /// AddressIdNumber is used to link to an Address entity.
    /// </summary>
    public class Contact: IContexted, ITaggable
    {
        public int ContactId { get; set; }
        public bool Active { get; set; }

        [MaxLength(256)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string MiddleName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string DisplayName { get; set; } = string.Empty;

        public int? AddressIdNumber { get; set; } 

        [MaxLength(256)]
        public string Email1 { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Email2 { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(256)]
        public string MobileNumber { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string Notes { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PictureLink { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string Website { get; set; } = string.Empty;
        public int AccessLevel { get; set; }
        public int ProgenyId { get; set; } = 0; // Which Progeny this contact belongs to. 0 if it belongs to a family
        public int FamilyId { get; set; } = 0; // Which Family this contact belongs to. 0 if it belongs to a Progeny

        [MaxLength(512)]
        public string Tags { get; set; } = string.Empty; // Comma separated list of tags.
        public DateTime? DateAdded { get; set; }

        [MaxLength(256)]
        public string Author { get; set; } // Todo: Replace with CreatedBy?

        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public Address Address { get; set; }

        [NotMapped]
        public string AddressString { get; set; } = string.Empty;

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
