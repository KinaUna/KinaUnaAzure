using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class VocabularyListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public VocabularyItem[] VocabularyList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
