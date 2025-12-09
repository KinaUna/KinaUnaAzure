using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Note data.
    /// </summary>
    public class Note: ICategorical
    {
        public int NoteId { get; set; }

        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } // Todo: Replace with CreatedTime?
        public int ProgenyId { get; set; }
        
        [MaxLength(256)]
        public string Owner { get; set; } = string.Empty; // Todo: Replace with CreatedBy?

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
        public Progeny Progeny { get; set; }

        [NotMapped]
        public int NoteNumber { get; set; }

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
    }
}
