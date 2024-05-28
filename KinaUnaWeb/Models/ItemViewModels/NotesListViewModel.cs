using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Notes;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NotesListViewModel: BaseItemsViewModel
    {
        public List<NoteViewModel> NotesList { get; init; }
        public NotesPageParameters NotesPageParameters { get; init; }
        public NotesListViewModel()
        {
            NotesList = [];
        }

        public NotesListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            NotesList = [];
            SetBaseProperties(baseItemsViewModel);

            NotesPageParameters = new NotesPageParameters
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
