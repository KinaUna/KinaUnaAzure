using System.Collections.Generic;

namespace KinaUna.Data.Models.Search
{
    /// <summary>
    /// Generic response model for search results.
    /// </summary>
    /// <typeparam name="T">The entity type being searched.</typeparam>
    public class SearchResponse<T>
    {
        /// <summary>
        /// The list of matching results for the current page.
        /// </summary>
        public List<T> Results { get; set; } = [];

        /// <summary>
        /// Total number of matching items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items remaining available to be fetched.
        /// </summary>
        public int RemainingItems { get; set; }
        /// <summary>
        /// The original search request parameters.
        /// </summary>
        public SearchRequest SearchRequest { get; set; }
    }
}
