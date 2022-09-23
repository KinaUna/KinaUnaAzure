using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        Task<MobileNotification> GetMobileNotification(int id);
        Task AddMobileNotification(MobileNotification notification);
        Task<MobileNotification> UpdateMobileNotification(MobileNotification notification);
        Task<MobileNotification> DeleteMobileNotification(MobileNotification notification);
        Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language);
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task<List<CalendarItem>> GetCalendarList(int id);
        Task<List<Location>> GetLocationsList(int id);
        Task<List<Friend>> GetFriendsList(int progenyId);
        Task<List<Contact>> GetContactsList(int progenyId);
        Task<UserInfo> GetUserInfoByUserId(string id);
        Task<UserInfo> GetUserInfoByEmail(string userEmail);
        Task<Progeny> GetProgeny(int id);
    }
}
