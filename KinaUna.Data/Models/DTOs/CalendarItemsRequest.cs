using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class CalendarItemsRequest
    {
        public List<int> ProgenyIds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int StartYear { get; set; }
        public int StartMonth { get; set; }
        public int StartDay { get; set; }
        public int EndYear { get; set; }
        public int EndMonth { get; set; }
        public int EndDay { get; set; }
    }
}
