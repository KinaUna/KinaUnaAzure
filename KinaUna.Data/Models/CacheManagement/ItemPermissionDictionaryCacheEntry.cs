using KinaUna.Data.Models.AccessManagement;
using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ItemPermissionDictionaryCacheEntry
    {
        public Dictionary<int, PermissionLevel> ItemPermissionDictionary { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
