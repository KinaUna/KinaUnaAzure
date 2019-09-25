using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class NotesListPage
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Note> NotesList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public NotesListPage()
        {
            NotesList = new List<Note>();
        }
    }
}
