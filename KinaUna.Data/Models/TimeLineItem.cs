﻿using System;
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
        public int ProgenyId { get; set; }
        public DateTime ProgenyTime { get; set; }
        public DateTime CreatedTime { get; set; }
        public int ItemType { get; set; }
        public string ItemId { get; set; }

        [MaxLength(256)]
        public string CreatedBy { get; set; }
        public int AccessLevel { get; set; }

        [NotMapped]
        public int ItemYear { get; set; } // For recurring events.

        [NotMapped]
        public int ItemMonth { get; set; } // For recurring events.

        [NotMapped]
        public int ItemDay { get; set; } // For recurring events.
    }
}
