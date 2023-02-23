using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoListViewModel: BaseItemsViewModel
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Video> VideosList { get; set; }
        public string TagFilter { get; set; }


        public VideoListViewModel()
        {
            
        }

        public VideoListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetPropertiesFromPageViewModel(VideoPageViewModel videoPageViewModel)
        {
            PageNumber = videoPageViewModel.PageNumber;
            TotalPages = videoPageViewModel.TotalPages;
            SortBy = videoPageViewModel.SortBy;
            TagFilter = videoPageViewModel.TagFilter;
            VideosList = videoPageViewModel.VideosList;
        }
    }
}
