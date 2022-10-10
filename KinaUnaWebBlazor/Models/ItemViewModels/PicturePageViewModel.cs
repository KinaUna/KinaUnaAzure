using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class PicturePageViewModel: BaseViewModel
    {
        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public int SortBy { get; set; } = 0;
        public List<Picture> PicturesList { get; set; } = new List<Picture>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public string TagFilter { get; set; } = "";
        public string TagsList { get; set; } = "";
    }
}
