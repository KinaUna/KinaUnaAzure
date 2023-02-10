using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public class WebNotificationsService: IWebNotificationsService
    {
        private readonly INotificationsHttpClient _notificationsHttpClient;
        public WebNotificationsService(INotificationsHttpClient notificationsHttpClient)
        {
            _notificationsHttpClient = notificationsHttpClient;
        }

        public async Task<WebNotification> SaveNotification(WebNotification notification)
        {
            notification = await _notificationsHttpClient.AddWebNotification(notification);

            return notification;
        }

        public async Task<WebNotification> UpdateNotification(WebNotification notification)
        {
            notification = await _notificationsHttpClient.UpdateWebNotification(notification);

            return notification;
        }

        public async Task RemoveNotification(WebNotification notification)
        {
            await _notificationsHttpClient.RemoveWebNotification(notification);
        }

        public async Task<WebNotification> GetNotificationById(int id)
        {
            WebNotification notification = await _notificationsHttpClient.GetWebNotificationById(id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await _notificationsHttpClient.GetUsersWebNotifications(userId);

            return usersNotifications;
        }
    }
}
