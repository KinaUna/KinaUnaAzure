namespace KinaUnaWeb.Models.TypeScriptModels.TodoItems
{
    public class TodoItemsPageParameters: BasePageParameters
    {
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
        /// Comma-separated list of contexts to filter by (e.g., "context1,context2").
        /// </summary>
        public string ContextFilter { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of status codes to filter by (e.g., "0,1,2" for Not started, In progress, Completed).
        /// </summary>
        public string StatusFilter { get; set; } = string.Empty;

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
    }
}
