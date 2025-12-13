using System;

namespace KinaUna.Data.Models.CacheManagement;

public class ProgenyOrFamilyUpdatedCacheEntry
{
    public int ProgenyId { get; set; }
    public int FamilyId { get; set; }
    public DateTime UpdateTime { get; set; }
}