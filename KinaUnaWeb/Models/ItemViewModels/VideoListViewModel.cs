using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Videos;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class VideoListViewModel: BaseItemsViewModel
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; init; }
        public int PreviousPage { get; set; }
        public int NextPage { get; set; }
        public int Back5Pages { get; set; }
        public int Forward5Pages { get; set; }
        public int PageNumberIfSortChanges { get; set; }
        public int SortBy { get; init; }
        public List<Video> VideosList { get; set; }
        public string TagFilter { get; init; }
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Day { get; set; } = 0;
        public VideosPageParameters VideosPageParameters { get; set; }
        public int VideoId { get; set; }

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
            VideosList = videoPageViewModel.VideosList;
            TagsList = videoPageViewModel.TagsList;

            PreviousPage = PageNumber - 1;
            NextPage = PageNumber + 1;
            if (PreviousPage < 1)
            {
                PreviousPage = TotalPages;
            }

            if (NextPage > TotalPages)
            {
                NextPage = 1;
            }

            Back5Pages = PageNumber - 5;
            Forward5Pages = PageNumber + 5;
            if (Back5Pages < 1)
            {
                Back5Pages = TotalPages;
            }

            if (Forward5Pages > TotalPages)
            {
                Forward5Pages = 1;
            }

            PageNumberIfSortChanges = TotalPages - PageNumber;
            if (PageNumberIfSortChanges < 1)
            {
                PageNumberIfSortChanges = 1;
            }

            if (PageNumber == 1)
            {
                PageNumberIfSortChanges = 1;
            }
        }
    }
}
