using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class ContactsListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public int FamilyId { get; set; }
        public List<Contact> ContactsList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
