﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Location data.
    /// </summary>
    public class Location: ILocatable, ITaggable
    {
        public int LocationId { get; set; }
        public int ProgenyId { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string County { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public DateTime? Date { get; set; }
        public string Notes { get; set; }

        public int AccessLevel { get; set; }
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public int LocationNumber { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }

        public string GetLocationString()
        {
            return Name;
        }
    }
}
