using System;

namespace KinaUna.Data.Models.AccessManagement
{
    public class HasItemPermissionCacheEntry
    {
        public TimelineItemPermission TimelineItemPermission { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
