using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.AccessManagement
{
    public class ItemPermissionDictionaryCacheEntry
    {
        public Dictionary<int, PermissionLevel> ItemPermissionDictionary { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
