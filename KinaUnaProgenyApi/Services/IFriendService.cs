using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IFriendService
    {
        /// <summary>
        /// Gets a Friend by FriendId.
        /// First tries to get the Friend from the cache.
        /// If the Friend isn't in the cache, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get.</param>
        /// <returns>Friend object. Null if the Friend entity doesn't exist.</returns>
        Task<Friend> GetFriend(int id);

        /// <summary>
        /// Adds a new Friend to the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to add.</param>
        /// <returns>The added Friend object.</returns>
        Task<Friend> AddFriend(Friend friend);

        /// <summary>
        /// Gets a Friend by FriendId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get and set.</param>
        /// <returns>The Friend object with the given FriendId. Null if the Friend entity doesn't exist.</returns>
        Task<Friend> SetFriendInCache(int id);

        /// <summary>
        /// Updates a Friend in the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object with the updated properties.</param>
        /// <returns>The updated Friend object.</returns>
        Task<Friend> UpdateFriend(Friend friend);

        /// <summary>
        /// Deletes a Friend from the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to delete.</param>
        /// <returns>The deleted Friend object.</returns>
        Task<Friend> DeleteFriend(Friend friend);

        /// <summary>
        /// Removes a Friend from the cache, then updates the cached list of Friends for the Progeny.
        /// </summary>
        /// <param name="id">The FriendId of the Friend to remove.</param>
        /// <param name="progenyId"></param>
        /// <returns></returns>
        Task RemoveFriendFromCache(int id, int progenyId);

        /// <summary>
        /// Gets a list of all Friends for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of Friends for.</param>
        /// <returns>List of Friends.</returns>
        Task<List<Friend>> GetFriendsList(int progenyId);
    }
}
