using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class PicturesLocationsResponse
    {
        public int ProgenyId { get; set; } = 0;
        public List<Location> LocationsList { get; set; }
        public int NumberOfLocations { get; set; }
    }
}
