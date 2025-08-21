using System.Collections.Generic;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaWeb.Models.TypeScriptModels.TodoItems
{
    public class SubtasksPageResponse(SubtasksResponse todoItemsResponse)
    {
        public int ParentTodoItemId { get; set; } = todoItemsResponse.ParentTodoItemId;
        /// <summary>
        /// Gets or sets the current page number for paginated results.
        /// </summary>
        public int PageNumber { get; set; } = todoItemsResponse.PageNumber;

        /// <summary>
        /// Gets or sets the total number of pages available in the response.
        /// </summary>
        public int TotalPages { get; set; } = todoItemsResponse.TotalPages;

        /// <summary>
        /// Gets or sets the total number of items in the response.
        /// </summary>
        public int TotalItems { get; set; } = todoItemsResponse.TotalItems;

        /// <summary>
        /// Gets or sets the list of TodoItems.
        /// </summary>
        public List<TodoItem> TodosList { get; set; } = todoItemsResponse.Subtasks;
    }
}
