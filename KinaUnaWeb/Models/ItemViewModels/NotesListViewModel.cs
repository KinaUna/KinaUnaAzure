using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotesListViewModel: BaseItemsViewModel
    {
        public List<NoteViewModel> NotesList { get; set; }
        
        public NotesListViewModel()
        {
            NotesList = new List<NoteViewModel>();
        }

        public NotesListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            NotesList = new List<NoteViewModel>();
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
