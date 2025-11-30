using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class MeasurementsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Measurement[] MeasurementsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
