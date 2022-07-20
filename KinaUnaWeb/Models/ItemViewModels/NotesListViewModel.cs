using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotesListViewModel: BaseViewModel
    {
        public List<NoteViewModel> NotesList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public NotesListViewModel()
        {
            NotesList = new List<NoteViewModel>();
        }
    }
}
