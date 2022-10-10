namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class UploadVideoViewModel: BaseViewModel
    {
        public IFormFile? File { get; set; }
        public int ProgenyId { get; set; } = 0;
        public string FileName { get; set; } = "";
        public string FileLink { get; set; } = "";
        public string ThumbLink { get; set; } = "";
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public string Owners { get; set; } = "";
        public int VideoType { get; set; } = 0;
        public DateTime? VideoTime { get; set; }
        public string DurationHours { get; set; } = "";
        public string DurationMinutes { get; set; } = "";
        public string DurationSeconds { get; set; } = "";
    }
}
