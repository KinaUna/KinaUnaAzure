using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class NotesListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Note[] NotesList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
