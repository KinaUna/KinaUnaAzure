using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class NearByPhotosRequest
    {
        /// <summary>
        /// The Progeny Id to search for photos.
        /// </summary>
        public int ProgenyId { get; set; } = 0;

        /// <summary>
        /// List of Progeny Ids to search for photos.
        /// </summary>
        public List<int> Progenies { get; set; } = [];

        /// <summary>
        /// The location to search for photos.
        /// </summary>
        public Location LocationItem { get; set; } = new Location();
        /// <summary>
        /// The distance in kilometers from the location to search for photos.
        /// </summary>
        public double Distance { get; set; } = 0.25;

        public int SortOrder { get; set; } = 1;
    }
}
