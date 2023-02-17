using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface IFriendsHttpClient
    {
        /// <summary>
        /// Gets the Friend with the given FriendId.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend (Friend.FriendId).</param>
        /// <returns>Friend</returns>
        Task<Friend> GetFriend(int friendId);

        /// <summary>
        /// Adds a new Friend.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to add.</param>
        /// <returns>Friend: The Friend object that was added.</returns>
        Task<Friend> AddFriend(Friend friend);

        /// <summary>
        /// Updates a Friend. The Friend with the same FriendId will be updated.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to update.</param>
        /// <returns>Friend: The updated Friend.</returns>
        Task<Friend> UpdateFriend(Friend friend);

        /// <summary>
        /// Removes the Friend with a given FriendId.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend to remove (Friend.FriendId).</param>
        /// <returns>bool: True if the Friend was successfully removed.</returns>
        Task<bool> DeleteFriend(int friendId);

        /// <summary>
        /// Gets the list of Friend objects for a given progeny that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <param name="tagFilter">string: The tag to filter by, returns all friends if empty.</param>
        /// <returns></returns>
        Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel, string tagFilter = "");
    }
}
