using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class TodosListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public TodoItem[] TodosList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
