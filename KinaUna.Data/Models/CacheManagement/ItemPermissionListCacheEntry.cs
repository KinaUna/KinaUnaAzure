using KinaUna.Data.Models.AccessManagement;
using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ItemPermissionListCacheEntry
    {
        public List<TimelineItemPermission> TimelineItemPermissions { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
