using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class NotesListViewModel: BaseViewModel
    {
        public List<NoteViewModel> NotesList { get; set; } = [];
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
    }
}
