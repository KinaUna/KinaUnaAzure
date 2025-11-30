using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class FriendsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Friend[] FriendsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
