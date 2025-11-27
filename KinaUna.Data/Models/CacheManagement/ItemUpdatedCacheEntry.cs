using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ItemUpdatedCacheEntry
    {
        public int ItemId { get; set; }
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
