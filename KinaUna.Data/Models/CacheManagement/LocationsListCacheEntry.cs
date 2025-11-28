using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class LocationsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public List<Location> LocationsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
