using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Timeline
{
    public class TimelineParameters
    {
        public int ProgenyId { get; set; } = 0;
        public List<int> Progenies { get; set; } = [];
        public int Skip { get; set; } = 0;
        public int Count { get; set; } = 5;
        public int SortBy { get; set; } = 1;
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Day { get; set; } = 0;
        public int FirstItemYear { get; set; } = 1900;
    }
}
