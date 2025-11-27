using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class NotesListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Note> NotesList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
