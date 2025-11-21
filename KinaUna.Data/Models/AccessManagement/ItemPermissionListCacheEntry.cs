using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.AccessManagement
{
    public class ItemPermissionListCacheEntry
    {
        public List<TimelineItemPermission> TimelineItemPermissions { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
