using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for TimelineItem data.
    /// ItemType corresponds to KinaUnaTypes.TimeLineType.
    /// </summary>
    public class TimeLineItem
    {
        [Key]
        public int TimeLineId { get; init; }
        public int ProgenyId { get; set; } = 0;
        public int FamilyId { get; set; } = 0;
        public DateTime ProgenyTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }
        public int ItemType { get; set; }
        [MaxLength(256)]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; }
        
        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        public int AccessLevel { get; set; }

        [NotMapped]
        public int ItemYear { get; set; } // For recurring events.

        [NotMapped]
        public int ItemMonth { get; set; } // For recurring events.

        [NotMapped]
        public int ItemDay { get; set; } // For recurring events.
    }
}
