using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    public class WebNotificationsService(IWebNotificationsHttpClient webNotificationsHttpClient) : IWebNotificationsService
    {
        public async Task<WebNotification> SaveNotification(WebNotification notification)
        {
            notification = await webNotificationsHttpClient.AddWebNotification(notification);

            return notification;
        }

        public async Task<WebNotification> UpdateNotification(WebNotification notification)
        {
            notification = await webNotificationsHttpClient.UpdateWebNotification(notification);

            return notification;
        }

        public async Task RemoveNotification(WebNotification notification)
        {
            await webNotificationsHttpClient.RemoveWebNotification(notification);
        }

        public async Task<WebNotification> GetNotificationById(int id)
        {
            WebNotification notification = await webNotificationsHttpClient.GetWebNotificationById(id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await webNotificationsHttpClient.GetUsersWebNotifications(userId);

            return usersNotifications;
        }

        public async Task<List<WebNotification>> GetLatestNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true)
        {
            List<WebNotification> latestNotifications = await webNotificationsHttpClient.GetLatestWebNotifications(userId, start, count, unreadOnly);

            return latestNotifications;
        }

        public async Task<int> GetUsersNotificationsCount(string userId)
        {
            int notificationsCount = await webNotificationsHttpClient.GetUsersNotificationsCount(userId);

            return notificationsCount;
        }
    }
}
