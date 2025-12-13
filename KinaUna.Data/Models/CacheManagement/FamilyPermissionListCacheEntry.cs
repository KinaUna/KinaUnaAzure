using KinaUna.Data.Models.AccessManagement;
using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class FamilyPermissionListCacheEntry
    {
        public List<FamilyPermission> FamilyPermissions { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
