using System;

namespace KinaUna.Data.Models.CacheManagement;

public class TimelineUpdatedCacheEntry
{
    public int ProgenyId { get; set; }
    public int FamilyId { get; set; }
    public KinaUnaTypes.TimeLineType TimeLineType { get; set; }
    public DateTime UpdateTime { get; set; }
}