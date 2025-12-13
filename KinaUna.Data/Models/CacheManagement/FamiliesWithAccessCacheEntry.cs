using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class FamiliesWithAccessCacheEntry
    {
        public List<int> FamilyIds { get; set; } = new();
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }
}
