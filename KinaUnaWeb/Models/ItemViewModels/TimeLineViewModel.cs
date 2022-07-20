using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineViewModel: BaseViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; set; }
        
        public int SortBy { get; set; }
        public int Items { get; set; }
        public Progeny Progeny { get; set; }
    }
}
