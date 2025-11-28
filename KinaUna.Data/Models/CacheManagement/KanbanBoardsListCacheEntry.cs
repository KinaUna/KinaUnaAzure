using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class KanbanBoardsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public List<KanbanBoard> KanbanBoardsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
