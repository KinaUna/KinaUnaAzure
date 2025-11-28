using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class MeasurementsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Measurement> MeasurementsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
