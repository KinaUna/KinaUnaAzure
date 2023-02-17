using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class FriendService: IFriendService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public FriendService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }
        
        public async Task<Friend> GetFriend(int id)
        {
            Friend friend = await GetFriendFromCache(id);
            if (friend == null || friend.FriendId == 0)
            {
                friend = await SetFriendInCache(id);
            }
            return friend;
        }

        private async Task<Friend> GetFriendFromCache(int id)
        {
            Friend friend = new Friend();
            string cachedFriend = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);
            if (!string.IsNullOrEmpty(cachedFriend))
            {
                friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            }

            return friend;
        }

        public async Task<Friend> SetFriendInCache(int id)
        {
            Friend friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (friend != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);

                _ = await SetFriendsListInCache(friend.ProgenyId);
            }

            return friend;
        }

        public async Task<Friend> AddFriend(Friend friend)
        {
            Friend friendToAdd = new Friend();
            friendToAdd.CopyPropertiesForAdd(friend);

            _ = _context.FriendsDb.Add(friendToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetFriendInCache(friendToAdd.FriendId);

            return friendToAdd;
        }
        

        public async Task<Friend> UpdateFriend(Friend friend)
        {
            Friend friendToUpdate = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == friend.FriendId);
            if (friendToUpdate != null)
            {
                friendToUpdate.ProgenyId = friend.ProgenyId;
                friendToUpdate.PictureLink = friend.PictureLink;
                friendToUpdate.Author = friend.Author;
                friendToUpdate.Name = friend.Name;
                friendToUpdate.Description = friend.Description;
                friendToUpdate.Context = friend.Context;
                friendToUpdate.FriendAddedDate = friend.FriendAddedDate;
                friendToUpdate.FriendSince = friend.FriendSince;
                friendToUpdate.Notes = friend.Notes;
                friendToUpdate.AccessLevel = friend.AccessLevel;
                friendToUpdate.Tags = friend.Tags;
                friendToUpdate.Type = friend.Type;
                friendToUpdate.Progeny = friend.Progeny;

                _ = _context.FriendsDb.Update(friendToUpdate);
                _ = await _context.SaveChangesAsync();
                _ = await SetFriendInCache(friend.FriendId);
            }
            

            return friend;
        }

        public async Task<Friend> DeleteFriend(Friend friend)
        {
            Friend friendToDelete = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == friend.FriendId);
            if (friendToDelete != null)
            {
                _ = _context.FriendsDb.Remove(friendToDelete);
                _ = await _context.SaveChangesAsync();
                await RemoveFriendFromCache(friend.FriendId, friend.ProgenyId);
            }
            
            return friend;
        }
        public async Task RemoveFriendFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);

            _ = await SetFriendsListInCache(progenyId);
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList = await GetFriendsListFromCache(progenyId);

            if (friendsList == null || !friendsList.Any())
            {
                friendsList = await SetFriendsListInCache(progenyId);
            }

            return friendsList;
        }

        private async Task<List<Friend>> GetFriendsListFromCache(int progenyId)
        {
            List<Friend> friendsList = new List<Friend>();
            string cachedFriendsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }

            return friendsList;
        }

        private async Task<List<Friend>> SetFriendsListInCache(int progenyId)
        {
            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);

            return friendsList;
        }
    }
}
