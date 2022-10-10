using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class NotesListViewModel: BaseViewModel
    {
        public List<NoteViewModel> NotesList { get; set; } = new List<NoteViewModel>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
    }
}
