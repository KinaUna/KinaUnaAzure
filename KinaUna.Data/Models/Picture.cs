using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace KinaUna.Data.Models
{
    public class Picture
    {
        [Required]
        public int PictureId { get; set; }
        [Required]
        [MaxLength(400)]
        public string PictureLink { get; set; }
        public string PictureLink600 { get; set; }
        public string PictureLink1200 { get; set; }
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; set; }
        public int PictureWidth { get; set; }
        public int PictureHeight { get; set; }

        public string Tags { get; set; }

        public string Location { get; set; }
        public string Longtitude { get; set; }
        public string Latitude { get; set; }
        public string Altitude { get; set; }

        public int ProgenyId { get; set; }
        [NotMapped]
        [JsonIgnore]
        public Progeny Progeny { get; set; }
        public string Owners { get; set; } // Comma separated list of emails.
        public string Author { get; set; }
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUsers, 4= public.
        public int CommentThreadNumber { get; set; }
        [NotMapped]
        public List<Comment> Comments { get; set; }
        [NotMapped]
        public string TimeZone { get; set; }
        [NotMapped]
        public int PictureNumber { get; set; }
    }
}
