using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class PictureViewModel: BaseViewModel
    {
        public int PictureId { get; set; } = 0;
        public string PictureLink { get; set; } = "";
        public DateTime? PictureTime { get; set; }
        public int? PictureRotation { get; set; }
        public int PictureWidth { get; set; } = 0;
        public int PictureHeight { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public Progeny? Progeny { get; set; } = new();
        public string Owners { get; set; } = ""; // Comma separated list of emails.
        public int AccessLevel { get; set; } = 5;// 0 = Hidden/Parents only, 1=Family, 2= Friends, 3=DefaultUSers, 4= public.
        public string Author { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public int CommentThreadNumber { get; set; } = 0;
        public List<Comment> CommentsList { get; set; } = [];
        public int CommentsCount { get; set; } = 0;
        public string Tags { get; set; } = "";
        public string TagFilter { get; set; } = "";
        public string TagsList { get; set; } = "";
        public string Location { get; set; } = "";
        public string Longitude { get; set; } = "";
        public string Longtitude { get; set; } = ""; // Todo: Fix typo.
        public string Latitude { get; set; } = "";
        public string Altitude { get; set; } = "";
        public List<Location> ProgenyLocations { get; set; } = [];
        public int PictureNumber { get; set; } = 0;
        public int PictureCount { get; set; } = 0;
        public int PrevPicture { get; set; } = 0;
        public int NextPicture { get; set; } = 0;
        public string PicTime { get; set; } = "";
        public bool PicTimeValid { get; set; } = false;
        public string PicYears { get; set; } = "";
        public string PicMonths { get; set; } = "";
        public string[] PicWeeks { get; set; } = new string[2];
        public string PicDays { get; set; } = "";
        public string PicHours { get; set; } = "";
        public string PicMinutes { get; set; } = "";
        public int SortBy { get; set; } = 0;
        public string UserId { get; set; } = "";
    }
}
