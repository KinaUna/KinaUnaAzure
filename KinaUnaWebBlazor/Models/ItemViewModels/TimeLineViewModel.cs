using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class TimeLineViewModel: BaseViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; set; } = [];
        public int SortBy { get; set; } = 0;
        public int Items { get; set; } = 0;
        public Progeny Progeny { get; set; } = new();
    }
}
