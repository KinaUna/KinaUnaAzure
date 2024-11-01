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
        public string VideoLink { get; set; }

        [MaxLength(1024)]
        public string ThumbLink { get; set; }
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
        public string Author { get; set; }

        [MaxLength(256)]
        public string Location { get; set; }

        [MaxLength(256)]
        public string Longtitude { get; set; }

        [MaxLength(256)]
        public string Latitude { get; set; }

        [MaxLength(256)]
        public string Altitude { get; set; }

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
