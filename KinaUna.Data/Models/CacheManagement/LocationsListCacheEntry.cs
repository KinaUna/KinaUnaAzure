using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class LocationsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public Location[] LocationsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
