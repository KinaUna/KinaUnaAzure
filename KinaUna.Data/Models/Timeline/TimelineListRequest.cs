using System.Collections.Generic;

namespace KinaUna.Data.Models.Timeline
{
    public class TimelineListRequest
    {
        public List<int> Families { get; set; } = [];
        public List<int> Progenies { get; set; } = [];
        public int SortOrder = 0;
        public int Skip { get; set; } = 0;
        public int Count { get; set; } = 5;
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Day { get; set; } = 0;
    }
}
