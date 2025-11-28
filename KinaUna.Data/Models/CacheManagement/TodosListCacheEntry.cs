using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class TodosListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public List<TodoItem> TodosList { get; set; } = new();
        public DateTime UpdateTime { get; set; }
    }
}
