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
    }
}
