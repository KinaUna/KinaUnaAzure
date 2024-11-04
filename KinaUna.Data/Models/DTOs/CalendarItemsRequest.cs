using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class CalendarItemsRequest
    {
        public List<int> ProgenyIds { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
