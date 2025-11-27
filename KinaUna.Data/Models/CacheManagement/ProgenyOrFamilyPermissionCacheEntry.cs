using System;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ProgenyOrFamilyPermissionCacheEntry
    {
        public bool HasPermission { get; set; } = false;
        public ProgenyPermission ProgenyPermission { get; set; } = new();
        public FamilyPermission FamilyPermission { get; set; } = new();

        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }
}
