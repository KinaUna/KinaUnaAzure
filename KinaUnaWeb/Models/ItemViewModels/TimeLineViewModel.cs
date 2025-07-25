using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Timeline;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineViewModel: BaseItemsViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; init; }
        public int SortBy { get; init; }
        public int Items { get; init; }
        public int Skip { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int FirstItemYear { get; set; }
        public TimelineParameters Parameters { get; set; }

        public TimeLineViewModel()
        {
            
        }

        public TimeLineViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetParametersFromProperties()
        {
            Parameters = new TimelineParameters
            {
                ProgenyId = CurrentProgeny.Id,
                Skip = Skip,
                Count = Items,
                SortBy = SortBy,
                Year = Year,
                Month = Month,
                Day = Day,
                FirstItemYear = FirstItemYear
            };
        }
    }
}
