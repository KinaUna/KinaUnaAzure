using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class FriendsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Friend> FriendsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
