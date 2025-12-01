using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class RecurrenceRulesListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public RecurrenceRule[] RecurrenceRulesList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
