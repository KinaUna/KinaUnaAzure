using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class TimelineRequest
    {
        public int ProgenyId { get; set; } = 0;
        public List<int> Progenies { get; set; } = [];
        public DateTime TimelineStartDateTime
        {
            get
            {
                if (Year == 0 && Month == 0 && Day == 0)
                {
                    DateTime now = DateTime.UtcNow;
                    Year = now.Year;
                    Month = now.Month;
                    Day = now.Day;
                }

                if (SortOrder == 1)
                {
                    return new DateTime(Year, Month, Day, 23, 59, 59);
                }

                return new DateTime(Year, Month, Day, 0, 0, 0);
            }
            set
            {
                Year = value.Year;
                Month = value.Month;
                Day = value.Day;
            }
        }

        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int FirstItemYear { get; set; } = 1900;
        public int AccessLevel { get; set; } = 5;
        public int Skip { get; set; } = 0;
        public int NumberOfItems { get; set; } = 10;
        public string TagFilter { get; set; } = string.Empty;
        public string CategoryFilter { get; set; } = string.Empty;
        public string ContextFilter { get; set; } = string.Empty;
        
        public List<KinaUnaTypes.TimeLineType> TimeLineTypeFilter { get; set; } = [];
        public int SortOrder { get; set; } = 1; // 0 = Ascending, 1 = Descending.
    }
}
