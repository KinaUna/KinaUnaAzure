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
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>Friend object. Null if the Friend entity doesn't exist.</returns>
        Task<Friend> GetFriend(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Friend to the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to set CreatedBy and ModifiedBy fields.</param>
        /// <returns>The added Friend object.</returns>
        Task<Friend> AddFriend(Friend friend, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Friend in the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object with the updated properties.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to set the ModifiedBy field.</param>
        /// <returns>The updated Friend object.</returns>
        Task<Friend> UpdateFriend(Friend friend, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Friend from the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to delete.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The deleted Friend object.</returns>
        Task<Friend> DeleteFriend(Friend friend, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Friends for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of Friends for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Friends.</returns>
        Task<List<Friend>> GetFriendsList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of friends associated with the specified progeny ID, filtered by a tag.
        /// </summary>
        /// <remarks>The method retrieves all friends associated with the specified progeny ID and filters
        /// the results based on the provided tag. The tag comparison is case-insensitive and culture-aware.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose friends are to be retrieved.</param>
        /// <param name="tag">An optional tag used to filter the friends. If null or empty, no filtering is applied.</param>
        /// <param name="currentUserInfo">The user information of the current user, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of friends associated
        /// with the specified progeny ID, filtered by the specified tag if provided.</returns>
        Task<List<Friend>> GetFriendsWithTag(int progenyId, string tag, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of friends associated with the specified progeny, filtered by context.
        /// </summary>
        /// <remarks>This method retrieves all friends associated with the specified progeny and filters
        /// them by the  provided context string, if any. The filtering is case-insensitive and matches substrings
        /// within  the context field of each friend.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose friends are to be retrieved.</param>
        /// <param name="context">A string used to filter the friends by context. Only friends whose context contains this value 
        /// (case-insensitive) will be included. If null or empty, no filtering is applied.</param>
        /// <param name="currentUserInfo">The user information of the current caller, used for authorization and context.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of friends  associated
        /// with the specified progeny, filtered by the provided context if applicable.</returns>
        Task<List<Friend>> GetFriendsWithContext(int progenyId, string context, UserInfo currentUserInfo);
    }
}
