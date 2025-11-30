using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ContactsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public Contact[] ContactsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
