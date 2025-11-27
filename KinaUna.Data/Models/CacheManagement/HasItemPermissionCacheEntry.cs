using KinaUna.Data.Models.AccessManagement;
using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class HasItemPermissionCacheEntry
    {
        public TimelineItemPermission TimelineItemPermission { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
