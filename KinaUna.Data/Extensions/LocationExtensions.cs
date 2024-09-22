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
            double result = Distance(location.Latitude, latitude, location.Longitude, longitude);
            return result;
        }


        //Source: https://learn.microsoft.com/en-us/answers/questions/1345224/calculate-distance-between-two-coordinates-lat1-lo
        private static double ToRadians(double angleIn10thofaDegree)
        {
            // Angle in 10th
            // of a degree
            return (angleIn10thofaDegree *
                    Math.PI) / 180;
        }

        // Source: https://learn.microsoft.com/en-us/answers/questions/1345224/calculate-distance-between-two-coordinates-lat1-lo
        private static double Distance(double lat1,
            double lat2,
            double lon1,
            double lon2)
        {

            // The math module contains
            // a function named toRadians
            // which converts from degrees
            // to radians.
            lon1 = ToRadians(lon1);
            lon2 = ToRadians(lon2);
            lat1 = ToRadians(lat1);
            lat2 = ToRadians(lat2);

            // Haversine formula
            double dlon = lon2 - lon1;
            double dlat = lat2 - lat1;
            double a = Math.Pow(Math.Sin(dlat / 2), 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Pow(Math.Sin(dlon / 2), 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));

            // Radius of earth in
            // kilometers. Use 3956
            // for miles
            double r = 6371;

            // calculate the result
            double result = c * r;
            return result;
        }
    }
}
