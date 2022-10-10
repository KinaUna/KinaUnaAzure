namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class LocationDataViewModel
    {
        public string Location { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Altitude { get; set; }

        public LocationDataViewModel()
        {
            Location = "";
            Latitude = "";
            Longitude = "";
            Altitude = "";
        }
    }
}
