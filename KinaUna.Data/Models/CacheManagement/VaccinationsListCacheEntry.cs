using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class VaccinationsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Vaccination[] VaccinationsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
