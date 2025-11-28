using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class VocabularyListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<VocabularyItem> VocabularyList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
