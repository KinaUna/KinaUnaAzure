using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class SkillsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Skill> SkillsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
