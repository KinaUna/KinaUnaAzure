using System;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ItemPermissionCacheEntry
    {
        public KinaUnaTypes.TimeLineType ItemType { get; set; }
        public int ItemId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public TimelineItemPermission ItemPermission { get; set; }
        public DateTime UpdateTime { get; set; }

    }
}
