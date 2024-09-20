using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class TimelineResponse
    {
        public List<TimeLineItem> TimeLineItems { get; set; } = [];
        public TimelineRequest Request { get; set; } = new TimelineRequest();
        public int RemainingItemsCount { get; set; } = 0;
    }
}
