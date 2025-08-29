using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class KanbanBoardsRequest
    {
        /// <summary>
        /// Gets or sets the list of Ids for the progenies to get KanbanBoards for.
        /// </summary>
        public List<int> ProgenyIds { get; set; } = [];

        /// <summary>
        /// Gets or sets the number of items to skip for pagination.
        /// </summary>
        public int Skip { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of items to retrieve for pagination.
        /// </summary>
        public int NumberOfItems { get; set; } = 10;

        /// <summary>
        /// Comma-separated list of tags to filter by (e.g., "tag1,tag2").
        /// </summary>
        public string TagFilter { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of contexts to filter by (e.g., "context1,context2").
        /// </summary>
        public string ContextFilter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether deleted items should be included in the results.
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;
    }
}
