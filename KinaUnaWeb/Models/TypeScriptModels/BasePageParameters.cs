using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class BasePageParameters
    {
        /// <summary>
        /// Gets the unique identifier for the progeny associated with this entity.
        /// </summary>
        public int ProgenyId { get; init; }

        /// <summary>
        /// Gets or sets the list of progeny entities.
        /// </summary>
        public List<int> Progenies { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the language.
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the current page number in a paginated collection or document.
        /// </summary>
        public int CurrentPageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items displayed per page in a paginated view.
        /// </summary>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages available.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the sort order value for the item. 0 for ascending, 1 for descending.
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// Comma-separated list of tags to filter by (e.g., "tag1,tag2").
        /// </summary>
        public string TagFilter { get; set; }
    }
}
