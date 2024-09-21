using System;
using Location = KinaUna.Data.Models.Location;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Location class.
    /// </summary>
    public static class LocationExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Location entity from one Location object to another.
        /// </summary>
        /// <param name="currentLocation"></param>
        /// <param name="otherLocation"></param>
        public static void CopyPropertiesForUpdate(this Location currentLocation, Location otherLocation )
        {
            currentLocation.AccessLevel = otherLocation.AccessLevel;
            currentLocation.Author = otherLocation.Author;
            currentLocation.City = otherLocation.City;
            currentLocation.Country = otherLocation.Country;
            currentLocation.County = otherLocation.County;
            currentLocation.Date = otherLocation.Date;
            currentLocation.DateAdded = otherLocation.DateAdded;
            currentLocation.District = otherLocation.District;
            currentLocation.HouseNumber = otherLocation.HouseNumber;
            currentLocation.Latitude = otherLocation.Latitude;
            currentLocation.Longitude = otherLocation.Longitude;
            currentLocation.Name = otherLocation.Name;
            currentLocation.Notes = otherLocation.Notes;
            currentLocation.PostalCode = otherLocation.PostalCode;
            currentLocation.ProgenyId = otherLocation.ProgenyId;
            currentLocation.State = otherLocation.State;
            currentLocation.StreetName = otherLocation.StreetName;
            currentLocation.Tags = otherLocation.Tags;
        }

        /// <summary>
        /// Copies the properties needed for adding a Location entity from one Location object to another.
        /// </summary>
        /// <param name="currentLocation"></param>
        /// <param name="otherLocation"></param>
        public static void CopyPropertiesForAdd(this Location currentLocation, Location otherLocation)
        {
            currentLocation.AccessLevel = otherLocation.AccessLevel;
            currentLocation.Author = otherLocation.Author;
            currentLocation.City = otherLocation.City;
            currentLocation.Country = otherLocation.Country;
            currentLocation.County = otherLocation.County;
            currentLocation.Date = otherLocation.Date;
            currentLocation.DateAdded = otherLocation.DateAdded;
            currentLocation.District = otherLocation.District;
            currentLocation.HouseNumber = otherLocation.HouseNumber;
            currentLocation.Latitude = otherLocation.Latitude;
            currentLocation.Longitude = otherLocation.Longitude;
            currentLocation.Name = otherLocation.Name;
            currentLocation.Notes = otherLocation.Notes;
            currentLocation.PostalCode = otherLocation.PostalCode;
            currentLocation.ProgenyId = otherLocation.ProgenyId;
            currentLocation.State = otherLocation.State;
            currentLocation.StreetName = otherLocation.StreetName;
            currentLocation.Tags = otherLocation.Tags;
        }

        /// <summary>
        /// Calculates the distance between two Location objects in meters.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns>Double with the distance in meters.</returns>
        public static double Distance(this Location location, double latitude, double longitude)
        {
            // Source: https://stackoverflow.com/questions/6366408/calculating-distance-between-two-latitude-and-longitude-geocoordinates
            double d1 = latitude * (Math.PI / 180.0);
            double num1 = longitude * (Math.PI / 180.0);
            double d2 = location.Latitude * (Math.PI / 180.0);
            double num2 = location.Longitude * (Math.PI / 180.0) - num1;
            double d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}
