using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class PicturesLocationsRequest
    {
        public int ProgenyId { get; set; } = 0;
        public List<int> Progenies { get; set; } = [];
        public double Distance { get; set; } = 0.25;
    }
}
