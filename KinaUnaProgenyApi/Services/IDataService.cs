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
    }
}
