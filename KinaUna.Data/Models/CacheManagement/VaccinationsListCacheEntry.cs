using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class VaccinationsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Vaccination> VaccinationsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
