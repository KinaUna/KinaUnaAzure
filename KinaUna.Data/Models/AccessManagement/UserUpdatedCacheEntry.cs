using System;

namespace KinaUna.Data.Models.AccessManagement
{
    public class UserUpdatedCacheEntry
    {
        public string UserId { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class ProgenyUpdatedCacheEntry
    {
        public int ProgenyId { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
