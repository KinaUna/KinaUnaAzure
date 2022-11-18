using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IFriendService
    {
        Task<Friend> GetFriend(int id);
        Task<Friend> AddFriend(Friend friend);
        Task<Friend> SetFriendInCache(int id);
        Task<Friend> UpdateFriend(Friend friend);
        Task<Friend> DeleteFriend(Friend friend);
        Task RemoveFriendFromCache(int id, int progenyId);
        Task<List<Friend>> GetFriendsList(int progenyId);
    }
}
