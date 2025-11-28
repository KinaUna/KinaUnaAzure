using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class SleepListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Sleep> SleepList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
