using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Locations API and Address API.
    /// </summary>
    public interface ILocationsHttpClient
    {
        /// <summary>
        /// Gets the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">The LocationId of the Location.</param>
        /// <returns>Location object with the given LocationId.</returns>
        Task<Location> GetLocation(int locationId);

        /// <summary>
        /// Adds a new Location.
        /// </summary>
        /// <param name="location">The Location to be added.</param>
        /// <returns>The Location object that was added.</returns>
        Task<Location> AddLocation(Location location);

        /// <summary>
        /// Updates a Location. The Location with the same LocationId will be updated.
        /// </summary>
        /// <param name="location">The Location to update.</param>
        /// <returns>The updated Location object.</returns>
        Task<Location> UpdateLocation(Location location);

        /// <summary>
        /// Removes the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">The LocationId of the Location to remove.</param>
        /// <returns>bool: True if the Location was successfully removed.</returns>
        Task<bool> DeleteLocation(int locationId);

        /// <summary>
        /// Gets the list of all locations for a Progeny that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the list of Locations for a progeny that a user has access to with a given tag.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="tagFilter">The string to filter the result list by. An empty string will include all locations.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>> GetLocationsList(int progenyId, int accessLevel, string tagFilter = "");

        /// <summary>
        /// Gets the Address entity with a given AddressId.
        /// </summary>
        /// <param name="addressId">The AddressId of the address.</param>
        /// <returns>The Address with the given AddressId. If it isn't found a new Address object with AddressId = 0.</returns>
        Task<Address> GetAddress(int addressId);

        /// <summary>
        /// Adds a new Address.
        /// </summary>
        /// <param name="address">The Address object to add.</param>
        /// <returns>The added Address object.</returns>
        Task<Address> AddAddress(Address address);

        /// <summary>
        /// Updates an Address. The Address with the same AddressId will be updated.
        /// </summary>
        /// <param name="address">The Address object with the updated properties.</param>
        /// <returns>The updated Address object.</returns>
        Task<Address> UpdateAddress(Address address);
    }
}
