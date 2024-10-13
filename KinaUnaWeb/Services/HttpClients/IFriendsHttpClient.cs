using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Friends API.
    /// </summary>
    public interface IFriendsHttpClient
    {
        /// <summary>
        /// Gets the Friend with the given FriendId.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend to get.</param>
        /// <returns>Friend object with the given FriendId. If not found, a new Friend object with FriendId = 0.</returns>
        Task<Friend> GetFriend(int friendId);

        /// <summary>
        /// Adds a new Friend item to the database.
        /// </summary>
        /// <param name="friend">The Friend object to add.</param>
        /// <returns>The Friend object that was added.</returns>
        Task<Friend> AddFriend(Friend friend);

        /// <summary>
        /// Updates a Friend item. The Friend item with the same FriendId will be updated.
        /// </summary>
        /// <param name="friend">The Friend object to update.</param>
        /// <returns>The updated Friend object.</returns>
        Task<Friend> UpdateFriend(Friend friend);

        /// <summary>
        /// Removes a Friend item with a given FriendId.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend item to remove.</param>
        /// <returns>bool: True if the Friend was successfully removed.</returns>
        Task<bool> DeleteFriend(int friendId);

        /// <summary>
        /// Gets the list of Friend objects for a given progeny that a user with a given access level has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <param name="tagFilter">The tag to filter by. Only Friend items with the tagFilter string in the Tag property are included. Includes all friends if tagFilter is an empty string.</param>
        /// <returns>List of Friend objects.</returns>
        Task<List<Friend>> GetFriendsList(int progenyId, string tagFilter = "");
    }
}
