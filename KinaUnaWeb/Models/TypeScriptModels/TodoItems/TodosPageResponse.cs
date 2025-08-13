using System.Collections.Generic;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.TypeScriptModels.TodoItems
{
    public class TodosPageResponse(TodoItemsResponse todoItemsResponse)
    {
        public int PageNumber { get; set; } = todoItemsResponse.PageNumber;
        public int TotalPages { get; set; } = todoItemsResponse.TotalPages;
        public int TotalItems { get; set; } = todoItemsResponse.TotalItems;
        public List<TodoItem> TodosList { get; set; } = todoItemsResponse.TodoItems;
        public List<string> TagsList { get; set; } = todoItemsResponse.TagsList;
        public List<string> ContextsList { get; set; } = todoItemsResponse.ContextsList;
    }
}
