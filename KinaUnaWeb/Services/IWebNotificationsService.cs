using System.Collections.Generic;
using KinaUna.Data.Models;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IWebNotificationsService
    {
        Task<WebNotification> SaveNotification(WebNotification notification);
        Task<WebNotification> UpdateNotification(WebNotification notification);
        Task RemoveNotification(WebNotification notification);
        Task<WebNotification> GetNotificationById(int id);

        Task<List<WebNotification>> GetUsersNotifications(string userId);
        Task<List<WebNotification>> GetLatestNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true);
        Task<int> GetUsersNotificationsCount(string userId);
    }
}
