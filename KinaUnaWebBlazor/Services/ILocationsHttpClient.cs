using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface ILocationsHttpClient
    {
        /// <summary>
        /// Gets the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location (Location.LocationId).</param>
        /// <returns>Location</returns>
        Task<Location?> GetLocation(int locationId);

        /// <summary>
        /// Adds a new Location.
        /// </summary>
        /// <param name="location">Location: The Location to be added.</param>
        /// <returns>Location: The Location object that was added.</returns>
        Task<Location?> AddLocation(Location? location);

        /// <summary>
        /// Updates a Location. The Location with the same LocationId will be updated.
        /// </summary>
        /// <param name="location">Location: The Location to update.</param>
        /// <returns>Location: The updated Location object.</returns>
        Task<Location?> UpdateLocation(Location? location);

        /// <summary>
        /// Removes the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location to remove (Location.LocationId).</param>
        /// <returns>bool: True if the Location was successfully removed.</returns>
        Task<bool> DeleteLocation(int locationId);

        /// <summary>
        /// Gets the list of locations for a Progeny that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>?> GetProgenyLocations(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the list of Locations for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>?> GetLocationsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the address with a given AddressId.
        /// </summary>
        /// <param name="addressId">int: The Id of the address (Address.AddressId).</param>
        /// <returns>Address</returns>
        Task<Address?> GetAddress(int addressId);

        /// <summary>
        /// Adds a new Address.
        /// </summary>
        /// <param name="address">Address: The Address object to add.</param>
        /// <returns>Address: The added Address object.</returns>
        Task<Address?> AddAddress(Address address);

        /// <summary>
        /// Updates an Address. The Address with the same AddressId will be updated.
        /// </summary>
        /// <param name="address">Address: The Address object to update.</param>
        /// <returns>Address: The updated Address object.</returns>
        Task<Address?> UpdateAddress(Address address);
    }
}
