using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class OnThisDayRequest
    {
        public int ProgenyId { get; set; } = 0;
        public DateTime ThisDayDateTime { get; set; } = DateTime.UtcNow;
        public int AccessLevel { get; set; } = 5;
        public int Skip { get; set; } = 0;
        public int NumberOfItems { get; set; } = 10;
        public string TagFilter { get; set; } = string.Empty;
        public OnThisDayPeriod OnThisDayPeriod { get; set; } = OnThisDayPeriod.Year;
        public List<KinaUnaTypes.TimeLineType> TimeLineTypeFilter { get; set; } = [];
    }
}
