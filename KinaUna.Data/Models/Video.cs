using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KinaUna.Data.Models.ItemInterfaces;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for Video data.
    /// </summary>
    public class Video: ITaggable, ILocatable
    {
        [Key]
        public int VideoId { get; set; }
        public DateTime? VideoTime { get; set; }

        [MaxLength(1024)]
        public string VideoLink { get; set; } // This is the link to the video file, YouTube link or OneDrive link.

        [MaxLength(1024)]
        public string ThumbLink { get; set; } // This is the link to the thumbnail image for the video.
        public int ProgenyId { get; set; }

        [MaxLength(2048)]
        public string Owners { get; set; } // Comma separated list of emails.
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public int CommentThreadNumber { get; set; }
        public int VideoType { get; set; } // 0 = file upload, 1 = OneDrive, 2 = Youtube

        [MaxLength(1024)]
        public string Tags { get; set; }
        public TimeSpan? Duration { get; set; }

        [MaxLength(256)]
        public string Author { get; set; } = string.Empty; // Todo: Replace with CreatedBy?

        [MaxLength(256)]
        public string Location { get; set; }

        [MaxLength(256)]
        public string Longtitude { get; set; } // Todo: Spell check - should be Longitude.

        [MaxLength(256)]
        public string Latitude { get; set; }

        [MaxLength(256)]
        public string Altitude { get; set; }

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
        public List<Comment> Comments { get; set; }
        [NotMapped]
        public string TimeZone { get; set; }
        [NotMapped]
        public int VideoNumber { get; set; }
        [NotMapped]
        public string DurationHours { get; set; }
        [NotMapped]
        public string DurationMinutes { get; set; }
        [NotMapped]
        public string DurationSeconds { get; set; }

        public string GetLocationString()
        {
            return Location;
        }
    }
}
