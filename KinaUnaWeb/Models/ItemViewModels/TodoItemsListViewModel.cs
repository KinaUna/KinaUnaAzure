using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.TodoItems;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TodoItemsListViewModel: BaseItemsViewModel
    {
        public List<TodoItem> TodoItemsList { get; set; }
        public TodoItemsPageParameters TodoItemsPageParameters { get; init; }
        
        public int PopUpTodoItemId = 0;

        public TodoItemsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);

            TodoItemsList = [];

            TodoItemsPageParameters = new TodoItemsPageParameters
            {
                ProgenyId = CurrentProgenyId,
                CurrentPageNumber = 0,
                ItemsPerPage = 10, // Get all items by default.
                TotalPages = 0,
                TotalItems = 0,
                LanguageId = LanguageId,
                TagFilter = "",
                ContextFilter = "",
                StatusFilter = "0, 1, 2", // Not started, In progress, Completed
                Sort = 0,
                SortBy = 0, // Sort by DueDate by default
                GroupBy = 1 // Group by Status by default
            };
        }
    }
}
