using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ILocationService
    {
        /// <summary>
        /// Gets a Location by LocationId.
        /// First tries to get the Location from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The LocationId of the Location to get.</param>
        /// <returns>The Location with the given LocationId. Null if the Location doesn't exist.</returns>
        Task<Location> GetLocation(int id);

        /// <summary>
        /// Adds a new Location to the database and the cache.
        /// </summary>
        /// <param name="location">The Location object to add.</param>
        /// <returns>The added Location object.</returns>
        Task<Location> AddLocation(Location location);

        /// <summary>
        /// Updates a Location in the database and the cache.
        /// </summary>
        /// <param name="location">Location object with the updated properties.</param>
        /// <returns>The updated Location object.</returns>
        Task<Location> UpdateLocation(Location location);

        /// <summary>
        /// Deletes a Location from the database and the cache.
        /// Then deletes the Location from the LocationsList cached for the Progeny.
        /// </summary>
        /// <param name="location">The Location to delete.</param>
        /// <returns>The deleted Location object.</returns>
        Task<Location> DeleteLocation(Location location);

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="accessLevel">The access level of the user.</param>
        /// <returns>List of Locations.</returns>
        Task<List<Location>> GetLocationsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets an Address by AddressId.
        /// First tries to get the Address from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address item to get.</param>
        /// <returns>Address object with the given AddressId.</returns>
        Task<Address> GetAddressItem(int id);

        /// <summary>
        /// Adds a new Address to the database and the cache.
        /// </summary>
        /// <param name="addressItem">The Address object to add.</param>
        /// <returns>The added Address object.</returns>
        Task<Address> AddAddressItem(Address addressItem);

        /// <summary>
        /// Updates an Address in the database and the cache.
        /// </summary>
        /// <param name="addressItem">The Address object with the updated properties.</param>
        /// <returns>The updated Address object.</returns>
        Task<Address> UpdateAddressItem(Address addressItem);

        /// <summary>
        /// Deletes an Address from the database and the cache.
        /// </summary>
        /// <param name="id">The AddressId of the Address to delete.</param>
        /// <returns></returns>
        Task RemoveAddressItem(int id);

        Task<List<Location>> GetLocationsWithTag(int progenyId, string tag, int accessLevel);
    }
}
