using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    /// <summary>
    /// Represents the response containing a collection of to-do items and related data.
    /// </summary>
    /// <remarks>This class encapsulates the result of a request for to-do items, including the list of to-do
    /// items, associated progeny data, and the original request details. It is typically used to transfer data between
    /// the client and server in a structured format.</remarks>
    public class TodoItemsResponse
    {
        /// <summary>
        /// Gets or sets the collection of to-do items.
        /// </summary>
        public List<TodoItem> TodoItems { get; set; }
        /// <summary>
        /// Gets or sets the list of progeny associated with the current list of to do items.
        /// </summary>
        public List<Progeny> ProgenyList { get; set; }

        /// <summary>
        /// Gets or sets the request object containing parameters used for retrieving the list of to-do items.
        /// </summary>
        public TodoItemsRequest TodoItemsRequest { get; set; }

        /// <summary>
        /// Gets or sets the current page number in a paginated collection.
        /// </summary>
        public int PageNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        public int TotalPages { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int TotalItems { get; set; } = 0;

        /// <summary>
        /// Gets or sets the list of tags associated with the current entity.
        /// </summary>
        public List<string> TagsList { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of context names.
        /// </summary>
        public List<string> ContextsList { get; set; } = [];
    }
}
