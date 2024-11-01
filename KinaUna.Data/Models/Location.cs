using System;
using System.ComponentModel.DataAnnotations;
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

        [MaxLength(512)]
        public string Name { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(512)]
        public string StreetName { get; set; }

        [MaxLength(128)]
        public string HouseNumber { get; set; }

        [MaxLength(256)]
        public string City { get; set; }

        [MaxLength(256)]
        public string District { get; set; }

        [MaxLength(256)]
        public string County { get; set; }

        [MaxLength(256)]
        public string State { get; set; }

        [MaxLength(256)]
        public string Country { get; set; }

        [MaxLength(256)]
        public string PostalCode { get; set; }
        public DateTime? Date { get; set; }
        public string Notes { get; set; }

        public int AccessLevel { get; set; }

        [MaxLength(512)]
        public string Tags { get; set; }
        public DateTime? DateAdded { get; set; }

        [MaxLength(256)]
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
