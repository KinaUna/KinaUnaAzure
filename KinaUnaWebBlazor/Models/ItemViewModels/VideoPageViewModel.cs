using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VideoPageViewModel: BaseViewModel
    {
        public int PageNumber { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public int SortBy { get; set; } = 0;
        public List<Video> VideosList { get; set; } = [];
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
        public string TagFilter { get; set; } = string.Empty;
        public string TagsList { get; set; } = string.Empty;
    }
}
