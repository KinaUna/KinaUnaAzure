using System.Collections.Generic;

namespace KinaUna.Data.Models.Search
{
    /// <summary>
    /// Request model for searching entities across progenies and families.
    /// </summary>
    public class SearchRequest
    {
        /// <summary>
        /// List of Progeny IDs to search within.
        /// </summary>
        public List<int> ProgenyIds { get; set; } = [];

        /// <summary>
        /// List of Family IDs to search within.
        /// </summary>
        public List<int> FamilyIds { get; set; } = [];

        /// <summary>
        /// The search query string. Searches are case-insensitive.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Number of items to skip for pagination.
        /// </summary>
        public int Skip { get; set; } = 0;

        /// <summary>
        /// Number of items to return. Use 0 for no limit.
        /// </summary>
        public int NumberOfItems { get; set; } = 25;

        /// <summary>
        /// Sort order. 0 = newest first (descending), 1 = oldest first (ascending).
        /// </summary>
        public int Sort { get; set; } = 0;
    }
}
