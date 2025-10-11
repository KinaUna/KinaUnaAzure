using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.ItemInterfaces;
using Newtonsoft.Json;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Picture data.
    /// </summary>
    public class Picture : ITaggable, ILocatable
    {
        [Required]
        public int PictureId { get; set; }
        [Required]
        [MaxLength(400)]
        public string PictureLink { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PictureLink600 { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PictureLink1200 { get; set; } = string.Empty;
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; set; }
        public int PictureWidth { get; set; }
        public int PictureHeight { get; set; }

        [MaxLength(256)]
        public string Tags { get; set; } = string.Empty; // Comma separated list of tags.

        [MaxLength(256)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(128)] public string Longtitude { get; set; } = string.Empty; // Todo: Spell check - should be Longitude.

        [MaxLength(128)]
        public string Latitude { get; set; } = string.Empty;

        [MaxLength(128)]
        public string Altitude { get; set; } = string.Empty;

        public int ProgenyId { get; set; }

        [MaxLength(1024)] public string Owners { get; set; } = string.Empty; // Comma separated list of emails.

        [MaxLength(256)]
        public string Author { get; set; } = string.Empty; // Todo: Replace with CreatedBy?
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.
        public int CommentThreadNumber { get; set; }

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

        [NotMapped] [JsonIgnore]
        public Progeny Progeny { get; set; }
        
        [NotMapped]
        public List<Comment> Comments { get; set; }
        
        [NotMapped]
        public string TimeZone { get; set; }
        
        [NotMapped]
        public int PictureNumber { get; set; }

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
            return Location;
        }
    }
}
