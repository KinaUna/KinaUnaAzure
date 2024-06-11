using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineViewModel: BaseItemsViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; init; }
        public int SortBy { get; init; }
        public int Items { get; init; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int FirstItemYear { get; set; }

        public TimeLineViewModel()
        {
            
        }

        public TimeLineViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
