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
        public List<KinaUnaTypes.TodoStatusType> StatusFilter { get; set; } = [];

        /// <summary>
        /// The sort order for the items. 0 for ascending and 1 for descending.
        /// </summary>
        public int Sort { get; set; } = 0; // Sort ascending = 0, Sort descending = 1

        /// <summary>
        /// Gets or sets the sorting criteria for items.
        /// 0 for DueDate, 1 for CreatedTime, 2 for StartDate, 3 for CompletedDate
        /// </summary>
        public int SortBy { get; set; } = 0;

        /// <summary>
        /// Gets or sets the grouping mode for data organization.
        /// 0 for no grouping, 1 for Status, 2 for Progeny
        /// </summary>
        public int GroupBy { get; set; } = 0;
        public string LocationFilter { get; set; }

        /// <summary>
        /// Sets the StartDate and EndDate properties based on the provided year, month, and day values.
        /// </summary>
        public void SetStartDateAndEndDate()
        {
            if (StartYear > 0 && StartMonth > 0 && StartDay > 0)
            {
                StartDate = new DateTime(StartYear, StartMonth, StartDay, 0, 0, 0);
            }
            else
            {
                StartDate = null;
            }

            if (EndYear > 0 && EndMonth > 0 && EndDay > 0)
            {
                EndDate = new DateTime(EndYear, EndMonth, EndDay, 23, 59, 59);
            }
            else
            {
                EndDate = null;
            }
        }
    }
}
