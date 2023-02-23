using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PicturesListViewModel: BaseItemsViewModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Picture> PicturesList { get; set; }
        public string TagFilter { get; set; }

        public PicturesListViewModel()
        {
            
        }

        public PicturesListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetPropertiesFromPageViewModel(PicturePageViewModel picturePageViewModel)
        {
            PageNumber = picturePageViewModel.PageNumber;
            PageSize = picturePageViewModel.PageSize;
            TotalPages = picturePageViewModel.TotalPages;
            SortBy = picturePageViewModel.SortBy;
            TagFilter = picturePageViewModel.TagFilter;
            PicturesList = picturePageViewModel.PicturesList;
        }
    }
}
