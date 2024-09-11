using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class OnThisDayResponse
    {
        public int ProgenyId { get; set; } = 0;
        public List<TimeLineItem> TimeLineItems { get; set; } = [];
        public string TagFilter {get; set; } = string.Empty;
    }
}
