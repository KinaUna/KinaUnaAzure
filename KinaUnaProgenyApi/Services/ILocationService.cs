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
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Location with the given LocationId. Null if the Location doesn't exist.</returns>
        Task<Location> GetLocation(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Location to the database and the cache.
        /// </summary>
        /// <param name="location">The Location object to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The added Location object.</returns>
        Task<Location> AddLocation(Location location, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Location in the database and the cache.
        /// </summary>
        /// <param name="location">Location object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Location object.</returns>
        Task<Location> UpdateLocation(Location location, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Location from the database and the cache.
        /// Then deletes the Location from the LocationsList cached for the Progeny.
        /// </summary>
        /// <param name="location">The Location to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Location object.</returns>
        Task<Location> DeleteLocation(Location location, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Locations for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Location entities for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Locations.</returns>
        Task<List<Location>> GetLocationsList(int progenyId, UserInfo currentUserInfo);

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

        /// <summary>
        /// Retrieves a list of locations associated with the specified progeny ID, filtered by a tag.
        /// </summary>
        /// <remarks>The method retrieves all locations for the specified progeny and filters them by the
        /// provided tag, if any.  The tag comparison is case-insensitive and culture-aware.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose locations are to be retrieved.</param>
        /// <param name="tag">An optional tag used to filter the locations. If null or empty, all locations are returned.</param>
        /// <param name="currentUserInfo">The user information of the current user, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Location"/>
        /// objects associated with the specified progeny ID, filtered by the specified tag if provided.</returns>
        Task<List<Location>> GetLocationsWithTag(int progenyId, string tag, UserInfo currentUserInfo);
    }
}
