using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class FamilyCacheEntry
    {
        public Family.Family Family { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
