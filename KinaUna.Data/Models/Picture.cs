using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        public string PictureLink { get; set; }

        [MaxLength(256)]
        public string PictureLink600 { get; set; }

        [MaxLength(256)]
        public string PictureLink1200 { get; set; }
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; set; }
        public int PictureWidth { get; set; }
        public int PictureHeight { get; set; }

        [MaxLength(256)]
        public string Tags { get; set; }

        [MaxLength(256)]
        public string Location { get; set; }

        [MaxLength(128)]
        public string Longtitude { get; set; }

        [MaxLength(128)]
        public string Latitude { get; set; }

        [MaxLength(128)]
        public string Altitude { get; set; }

        public int ProgenyId { get; set; }

        [MaxLength(1024)]
        public string Owners { get; set; } // Comma separated list of emails.

        [MaxLength(256)]
        public string Author { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.
        public int CommentThreadNumber { get; set; }

        [NotMapped] [JsonIgnore]
        public Progeny Progeny { get; set; }
        
        [NotMapped]
        public List<Comment> Comments { get; set; }
        
        [NotMapped]
        public string TimeZone { get; set; }
        
        [NotMapped]
        public int PictureNumber { get; set; }

        public string GetLocationString()
        {
            return Location;
        }
    }
}
