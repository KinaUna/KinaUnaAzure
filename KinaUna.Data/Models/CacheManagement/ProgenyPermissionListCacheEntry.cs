using KinaUna.Data.Models.AccessManagement;
using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ProgenyPermissionListCacheEntry
    {
        public List<ProgenyPermission> ProgenyPermissions { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
