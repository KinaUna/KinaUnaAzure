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
    }
}
