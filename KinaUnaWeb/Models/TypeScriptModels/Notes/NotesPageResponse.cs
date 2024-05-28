using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Notes
{
    public class NotesPageResponse
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<int> NotesList { get; set; } = [];
    }
}
