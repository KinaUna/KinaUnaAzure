using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class SkillsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Skill[] SkillsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
