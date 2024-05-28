using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class LocationViewModel: BaseViewModel
    {
        public int LocationId { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public string Name { get; set; } = "";
        public double Latitude { get; set; } = 0;
        public double Longitude { get; set; } = 0;
        public string StreetName { get; set; } = "";
        public string HouseNumber { get; set; } = "";
        public string City { get; set; } = "";
        public string District { get; set; } = "";
        public string County { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime? Date { get; set; }
        public int AccessLevel { get; set; } = 5;
        public string Tags { get; set; } = "";
        public DateTime? DateAdded { get; set; }
        public string Author { get; set; } = "";

        public List<Location> LocationsList { get; set; } = [];
        public string TagsList { get; set; } = "";
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; }
        public string TagFilter { get; set; } = "";
        public int? SortBy { get; set; }

        public Location Location { get; set; } = new();
    }
}
