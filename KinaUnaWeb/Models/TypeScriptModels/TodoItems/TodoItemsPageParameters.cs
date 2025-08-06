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

        public string ContextFilter { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
    }
}
