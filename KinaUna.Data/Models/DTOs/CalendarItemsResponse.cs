using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class CalendarItemsResponse
    {
        public List<CalendarItem> CalendarItems { get; set; }

        public List<Progeny> ProgenyList { get; set; }

        public CalendarItemsRequest CalendarItemsRequest { get; set; }
    }
}
