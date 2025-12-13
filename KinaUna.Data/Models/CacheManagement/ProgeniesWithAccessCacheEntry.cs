using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ProgeniesWithAccessCacheEntry
    {
        public List<int> ProgenyIds { get; set; } = [];
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }
}
