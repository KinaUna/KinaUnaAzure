using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class CalendarListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public CalendarItem[] CalendarItemsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
