using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Notes;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotesListViewModel: BaseItemsViewModel
    {
        public List<NoteViewModel> NotesList { get; set; }
        public NotesPageParameters NotesPageParameters { get; set; }
        public NotesListViewModel()
        {
            NotesList = new List<NoteViewModel>();
        }

        public NotesListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            NotesList = new List<NoteViewModel>();
            SetBaseProperties(baseItemsViewModel);

            NotesPageParameters = new()
            {
                ProgenyId = CurrentProgenyId,
                CurrentPageNumber = 0,
                ItemsPerPage = 10,
                TotalPages = 0,
                TotalItems = 0,
                LanguageId = LanguageId,
                TagFilter = "",
                Sort = 1
            };
        }
    }
}
