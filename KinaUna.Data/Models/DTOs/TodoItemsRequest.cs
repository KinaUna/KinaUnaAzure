using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    /// <summary>
    /// Represents a request for retrieving a filtered list of to-do items.
    /// </summary>
    /// <remarks>This class provides various filtering and pagination options for querying to-do items.
    /// Filters include date ranges, progeny IDs, tags, context, and status codes. Pagination is supported through the
    /// <see cref="Skip"/> and <see cref="NumberOfItems"/> properties.</remarks>
    public class TodoItemsRequest
    {
        /// <summary>
        /// Gets or sets the list of Ids for the progenies to get Todos for.
        /// </summary>
        public List<int> ProgenyIds { get; set; }
        /// <summary>
        /// Gets or sets the starting date of the items to include.
        /// </summary>
        public DateTime? StartDate { get; set; }
        /// <summary>
        /// Gets or sets the ending date of the items to include.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int StartYear { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int StartMonth { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int StartDay { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int EndYear { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int EndMonth { get; set; }

        /// <summary>
        /// The start and end dates are split into year, month, and day for easier filtering.
        /// </summary>
        public int EndDay { get; set; }
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
        /// Comma-separated list of status codes to filter by (e.g., "0,1,2" for Not started, In progress, Completed).
        /// </summary>
        public string StatusFilter { get; set; } = string.Empty;
    }
}
