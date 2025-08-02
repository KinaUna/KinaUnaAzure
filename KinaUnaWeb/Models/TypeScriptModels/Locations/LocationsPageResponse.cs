using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Locations
{
    public class LocationsPageResponse
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<Location> LocationsList { get; set; } = [];
        public List<string> TagsList { get; set; } = [];
    }
}
