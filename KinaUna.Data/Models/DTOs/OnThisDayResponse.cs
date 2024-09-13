using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class OnThisDayResponse
    {
        public List<TimeLineItem> TimeLineItems { get; set; } = [];
        public OnThisDayRequest Request { get; set; } = new OnThisDayRequest();
        public int RemainingItemsCount { get; set; } = 0;
    }
}
