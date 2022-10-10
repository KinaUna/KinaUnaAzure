using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VideoViewModel : BaseViewModel
    {
        public int VideoId { get; set; } = 0;
        public string VideoLink { get; set; } = string.Empty;
        public string ThumbLink { get; set; } = string.Empty;
        public DateTime? VideoTime { get; set; }
        public int? VideoRotation { get; set; }
        public int VideoNumber { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public Progeny Progeny { get; set; } = new Progeny();
        public string Owners { get; set; } = string.Empty; // Comma separated list of emails.
        public int AccessLevel { get; set; } = 5; // 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public string Author { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public int CommentThreadNumber { get; set; } = 0;
        public List<Comment> CommentsList { get; set; } = new List<Comment>();
        public int CommentsCount { get; set; } = 0;
        public int VideoType { get; set; } = 0;
        public string Tags { get; set; } = string.Empty;
        public string TagFilter { get; set; } = string.Empty;
        public string TagsList { get; set; } = string.Empty;
        public string DurationHours { get; set; } = string.Empty;
        public string DurationMinutes { get; set; } = string.Empty;
        public string DurationSeconds { get; set; } = string.Empty;
        public int SortBy { get; set; }
        public string UserId { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }

        public string Location { get; set; } = string.Empty;
        public string Longtitude { get; set; } = string.Empty;
        public string Latitude { get; set; } = string.Empty;
        public string Altitude { get; set; } = string.Empty;
        public List<Location> ProgenyLocations { get; set; } = new List<Location>();
        public int VideoCount { get; set; } = 0;
        public int PrevVideo { get; set; } = 0;
        public int NextVideo { get; set; } = 0;

        public string VidTime { get; set; } = string.Empty;
        public bool VidTimeValid { get; set; } = false;
        public string VidYears { get; set; } = string.Empty;
        public string VidMonths { get; set; } = string.Empty;
        public string[] VidWeeks { get; set; } = new string[2];
        public string VidDays { get; set; } = string.Empty;
        public string VidHours { get; set; } = string.Empty;
        public string VidMinutes { get; set; } = string.Empty;
    }
}
