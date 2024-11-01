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
        public string FirstName { get; set; }

        [MaxLength(256)]
        public string MiddleName { get; set; }

        [MaxLength(256)]
        public string LastName { get; set; }

        [MaxLength(256)]
        public string DisplayName { get; set; }

        public int? AddressIdNumber { get; set; }

        [MaxLength(256)]
        public string Email1 { get; set; }

        [MaxLength(256)]
        public string Email2 { get; set; }

        [MaxLength(256)]
        public string PhoneNumber { get; set; }

        [MaxLength(256)]
        public string MobileNumber { get; set; }

        [MaxLength(256)]
        public string Context { get; set; }

        [MaxLength(256)]
        public string Notes { get; set; }

        [MaxLength(1024)]
        public string PictureLink { get; set; }

        [MaxLength(1024)]
        public string Website { get; set; }
        public int AccessLevel { get; set; }
        public int ProgenyId { get; set; }

        [MaxLength(512)]
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }

        [MaxLength(256)]
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public Address Address { get; set; }

        [NotMapped]
        public string AddressString { get; set; }
    }
}
