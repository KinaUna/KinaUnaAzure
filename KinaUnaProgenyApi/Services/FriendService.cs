using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class FriendService : IFriendService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IImageStore _imageStore;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        
        public FriendService(ProgenyDbContext context, IDistributedCache cache, IImageStore imageStore, IAccessManagementService accessManagementService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _imageStore = imageStore;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Friend by FriendId.
        /// First tries to get the Friend from the cache.
        /// If the Friend isn't in the cache, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>Friend object. Null if the Friend entity doesn't exist.</returns>
        public async Task<Friend> GetFriend(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Friend friend = await GetFriendFromCache(id);
            friend ??= await SetFriendInCache(id);
            friend.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, friend.ProgenyId, 0, currentUserInfo);
            
            return friend;
        }

        /// <summary>
        /// Gets a Friend by FriendId from the cache.
        /// </summary>
        /// <param name="id">The FriendId of the Friend item to get.</param>
        /// <returns>Friend object. Null if the Friend item isn't found.</returns>
        private async Task<Friend> GetFriendFromCache(int id)
        {
            string cachedFriend = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);
            if (string.IsNullOrEmpty(cachedFriend))
            {
                return null;
            }

            Friend friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            return friend;
        }
        
        /// <summary>
        /// Gets a Friend by FriendId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get and set.</param>
        /// <returns>The Friend object with the given FriendId. Null if the Friend entity doesn't exist.</returns>
        private async Task<Friend> SetFriendInCache(int id)
        {
            Friend friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (friend == null) return null;
            
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);

            _ = await SetFriendsListInCache(friend.ProgenyId);

            return friend;
        }

        /// <summary>
        /// Adds a new Friend to the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to add.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The added Friend object.</returns>
        public async Task<Friend> AddFriend(Friend friend, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(friend.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            Friend friendToAdd = new();
            friendToAdd.CopyPropertiesForAdd(friend);

            _ = _context.FriendsDb.Add(friendToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, friend.ProgenyId, 0, friend.ItemPermissionsDtoList, currentUserInfo);
            
            _ = await SetFriendInCache(friendToAdd.FriendId);

            return friendToAdd;
        }


        /// <summary>
        /// Updates a Friend in the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object with the updated properties.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The updated Friend object.</returns>
        public async Task<Friend> UpdateFriend(Friend friend, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Friend friendToUpdate = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == friend.FriendId);
            if (friendToUpdate == null) return null;
            string oldPictureLink = friendToUpdate.PictureLink;

            friendToUpdate.CopyPropertiesForUpdate(friend);

            _ = _context.FriendsDb.Update(friendToUpdate);
            _ = await _context.SaveChangesAsync();

            if (oldPictureLink != friend.PictureLink)
            {
                List<Friend> friendsWithThisPicture = await _context.FriendsDb.AsNoTracking().Where(c => c.PictureLink == oldPictureLink).ToListAsync();
                if (friendsWithThisPicture.Count == 0)
                {
                    await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Friends);
                }
            }

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, friend.ProgenyId, 0, friend.ItemPermissionsDtoList, currentUserInfo);

            _ = await SetFriendInCache(friend.FriendId);

            return friend;
        }

        /// <summary>
        /// Deletes a Friend from the database and the cache.
        /// </summary>
        /// <param name="friend">The Friend object to delete.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The deleted Friend object.</returns>
        public async Task<Friend> DeleteFriend(Friend friend, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Friend friendToDelete = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == friend.FriendId);
            if (friendToDelete == null) return null;

            _ = _context.FriendsDb.Remove(friendToDelete);
            _ = await _context.SaveChangesAsync();
            await RemoveFriendFromCache(friend.FriendId, friend.ProgenyId);

            List<Friend> friendsWithThisPicture = await _context.FriendsDb.AsNoTracking().Where(f => f.PictureLink == friend.PictureLink).ToListAsync();
            if (friendsWithThisPicture.Count == 0)
            {
                _ = _imageStore.DeleteImage(friend.PictureLink, BlobContainers.Friends);
            }

            // Todo: Remove all associated permissions.
            return friend;
        }


        /// <summary>
        /// Removes a Friend from the cache, then updates the cached list of Friends for the Progeny.
        /// </summary>
        /// <param name="id">The FriendId of the Friend to remove.</param>
        /// <param name="progenyId"></param>
        /// <returns></returns>
        private async Task RemoveFriendFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);

            _ = await SetFriendsListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Friends for a Progeny from the cache.
        /// If the list is empty, it will be looked up in the database and added to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of Friends for.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Friends.</returns>
        public async Task<List<Friend>> GetFriendsList(int progenyId, UserInfo currentUserInfo)
        {
            List<Friend> friendsList = await GetFriendsListFromCache(progenyId);

            if (friendsList == null || friendsList.Count == 0)
            {
                friendsList = await SetFriendsListInCache(progenyId);
            }

            List<Friend> accessibleFriends = [];
            foreach (Friend friend in friendsList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, currentUserInfo, PermissionLevel.View))
                {
                    friend.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, friend.FriendId, friend.ProgenyId, 0, currentUserInfo);
                    accessibleFriends.Add(friend);
                }
            }

            return accessibleFriends;
        }

        /// <summary>
        /// Gets a list of all Friends for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Friends for.</param>
        /// <returns>List of Friends.</returns>
        private async Task<List<Friend>> GetFriendsListFromCache(int progenyId)
        {
            List<Friend> friendsList = [];
            string cachedFriendsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }

            return friendsList;
        }

        /// <summary>
        /// Gets a list of all Friends for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set the list ofFriends for.</param>
        /// <returns>List of Friends.</returns>
        private async Task<List<Friend>> SetFriendsListInCache(int progenyId)
        {
            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);

            return friendsList;
        }

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
        public async Task<List<Friend>> GetFriendsWithTag(int progenyId, string tag, UserInfo currentUserInfo)
        {
            List<Friend> allItems = await GetFriendsList(progenyId, currentUserInfo);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(f => f.Tags != null && f.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }

            return allItems;
        }

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
        public async Task<List<Friend>> GetFriendsWithContext(int progenyId, string context, UserInfo currentUserInfo)
        {
            List<Friend> allItems = await GetFriendsList(progenyId, currentUserInfo);
            if (!string.IsNullOrEmpty(context))
            {
                allItems = [.. allItems.Where(f => f.Context != null && f.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase))];
            }
            return allItems;
        }
    }
}
