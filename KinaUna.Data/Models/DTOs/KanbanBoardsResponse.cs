using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class KanbanBoardsResponse
    {
        public List<KanbanBoard> KanbanBoards { get; set; } = [];

        public List<Progeny> ProgenyList { get; set; } = [];

        public KanbanBoardsRequest KanbanBoardsRequest { get; set; }

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
    }
}
