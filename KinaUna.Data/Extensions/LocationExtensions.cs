using Location = KinaUna.Data.Models.Location;

namespace KinaUna.Data.Extensions
{
    public static class LocationExtensions
    {
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
    }
}
