﻿namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class LocationDataViewModel
    {
        public string Location { get; init; } = "";
        public string Latitude { get; set; } = "";
        public string Longitude { get; set; } = "";
        public string Altitude { get; set; } = "";
        public double LatitudeDouble { get; set; }
        public double LongitudeDouble { get; set; }
        public double AltitudeDouble { get; set; }
    }
}
