using System.Collections.Generic;

namespace KinaUnaWeb.Models.Kanbans
{
    public class KanbanBoardsListViewModel: BaseItemsViewModel
    {
        public List<KanbanBoard> KanbanBoardsList { get; set; }
        public KanbanBoardsPageParameters KanbanBoardsPageParameters { get; init; }
        public int PopUpKanbanBoardId = 0;
        public KanbanBoardsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            KanbanBoardsList = [];
            KanbanBoardsPageParameters = new KanbanBoardsPageParameters
            {
                ProgenyId = CurrentProgenyId,
                CurrentPageNumber = 0,
                ItemsPerPage = 10,
                TotalPages = 0,
                TotalItems = 0,
                LanguageId = LanguageId,
                Sort = 0,
            };
        }
    }
}
