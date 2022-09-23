using System;

namespace KinaUnaProgenyApi.Models.DTOs
{
    public class OldVideoDto
    {
        public int VideoId { get; set; }
        public DateTime? VideoTime { get; set; }
        public string VideoLink { get; set; }
        public string ThumbLink { get; set; }
        public int ProgenyId { get; set; }
        public string Owners { get; set; } // Comma separated list of emails.
        public int AccessLevel { get; set; } // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public int CommentThreadNumber { get; set; }
        public int VideoType { get; set; } // 0 = file upload, 1 = OneDrive, 2 = Youtube
        public string Tags { get; set; }
        public TimeSpan? Duration { get; set; }
        public string Author { get; set; }

    }
}
