using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Location data.
    /// </summary>
    public class Location: ILocatable, ITaggable
    {
        public int LocationId { get; set; }
        public int ProgenyId { get; set; } = 0;
        public int FamilyId { get; set; } = 0; 

        [MaxLength(512)]
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(512)]
        public string StreetName { get; set; } = string.Empty;

        [MaxLength(128)]
        public string HouseNumber { get; set; } = string.Empty;

        [MaxLength(256)]
        public string City { get; set; } = string.Empty;

        [MaxLength(256)]
        public string District { get; set; } = string.Empty;

        [MaxLength(256)]
        public string County { get; set; } = string.Empty;

        [MaxLength(256)]
        public string State { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Country { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PostalCode { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        [MaxLength(4096)]
        public string Notes { get; set; } = string.Empty;
        
        [MaxLength(512)]
        public string Tags { get; set; } = string.Empty; // Comma separated list of tags.
        public DateTime? DateAdded { get; set; }

        [MaxLength(256)] public string Author { get; set; } = string.Empty; // Todo: Replace with CreatedBy?

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
        public int LocationNumber { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
        [NotMapped]
        public Family.Family Family { get; set; }

        /// <summary>
        /// The current user's permissions for this item.
        /// </summary>
        [NotMapped]
        public TimelineItemPermission ItemPerMission { get; set; }

        /// <summary>
        /// Gets or sets the list of item permissions associated with the current entity. For adding or updating item permissions.
        /// </summary>
        [NotMapped]
        public List<ItemPermissionDto> ItemPermissionsDtoList { get; set; } = [];

        public string GetLocationString()
        {
            return Name;
        }
    }
}
