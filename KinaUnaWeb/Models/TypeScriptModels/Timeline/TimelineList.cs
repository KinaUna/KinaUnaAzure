using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Timeline
{
    public class TimelineList
    {
        public List<TimeLineItem> TimelineItems { get; set; } = [];
        public int AllItemsCount { get; set; } = 0;
        public int RemainingItemsCount { get; set; } = 0;
        public int FirstItemYear { get; set; } = 0;
    }
}
