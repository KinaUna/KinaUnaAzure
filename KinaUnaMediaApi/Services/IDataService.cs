using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Services
{
    public interface IDataService
    {
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task<UserInfo> GetUserInfoByUserId(string id);
        Task<UserInfo> GetUserInfoByEmail(string userEmail);
        Task<List<CalendarItem>> GetCalendarList(int id);
        Task<List<Location>> GetLocationsList(int id);
        Task<List<Friend>> GetFriendsList(int id);
        Task<List<Contact>> GetContactsList(int id);
        Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId);
        Task<Progeny> GetProgeny(int id);
        Task AddMobileNotification(MobileNotification notification);
    }
}
