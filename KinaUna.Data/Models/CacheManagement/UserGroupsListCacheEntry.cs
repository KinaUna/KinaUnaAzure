using System;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUna.Data.Models.CacheManagement
{
    public class UserGroupsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public UserGroup[] UserGroupsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
