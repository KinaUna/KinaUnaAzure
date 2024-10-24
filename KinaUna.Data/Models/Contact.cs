﻿using KinaUna.Data.Models.ItemInterfaces;
using System;
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
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public int? AddressIdNumber { get; set; }
        public string Email1 { get; set; }
        public string Email2 { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        public string Context { get; set; }
        public string Notes { get; set; }
        public string PictureLink { get; set; }
        public string Website { get; set; }
        public int AccessLevel { get; set; }
        public int ProgenyId { get; set; }
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }

        [NotMapped]
        public Address Address { get; set; }

        [NotMapped]
        public string AddressString { get; set; }
    }
}
