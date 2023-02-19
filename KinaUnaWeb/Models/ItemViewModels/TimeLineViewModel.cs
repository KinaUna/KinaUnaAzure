using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineViewModel: BaseItemsViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; set; }
        public int SortBy { get; set; }
        public int Items { get; set; }

        public TimeLineViewModel()
        {
            
        }

        public TimeLineViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
