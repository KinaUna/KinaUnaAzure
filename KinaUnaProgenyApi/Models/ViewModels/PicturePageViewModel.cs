using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models.ViewModels
{
    /// <summary>
    /// View model for showing a paginated list of Pictures.
    /// </summary>
    public class PicturePageViewModel
    {
        /// <summary>
        /// Current page number.
        /// </summary>
        public int PageNumber { get; set; }
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }
        /// <summary>
        /// Sort order, 0 = oldest first, 1 = newest first.
        /// </summary>
        public int SortBy { get; set; }
        /// <summary>
        /// The list of Pictures for the current page.
        /// </summary>
        public List<Picture> PicturesList { get; set; }
        /// <summary>
        /// The Progeny to show Pictures for.
        /// </summary>
        public Progeny Progeny { get; set; }
        /// <summary>
        /// Is the current user an admin for the Progeny.
        /// </summary>
        public bool IsAdmin { get; set; }
        /// <summary>
        /// Filter the list of Pictures by tag. Empty string = include all pictures.
        /// </summary>
        public string TagFilter { get; set; }
        /// <summary>
        /// List of tags (comma separated string) for all pictures for the Progeny, with the TagFilter applied. 
        /// </summary>
        public string TagsList { get; set; }
    }
}
