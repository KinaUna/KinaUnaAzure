using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    public class WebNotificationsService: IWebNotificationsService
    {
        private readonly IWebNotificationsHttpClient _webNotificationsHttpClient;
        public WebNotificationsService(IWebNotificationsHttpClient webNotificationsHttpClient)
        {
            _webNotificationsHttpClient = webNotificationsHttpClient;
        }

        public async Task<WebNotification> SaveNotification(WebNotification notification)
        {
            notification = await _webNotificationsHttpClient.AddWebNotification(notification);

            return notification;
        }

        public async Task<WebNotification> UpdateNotification(WebNotification notification)
        {
            notification = await _webNotificationsHttpClient.UpdateWebNotification(notification);

            return notification;
        }

        public async Task RemoveNotification(WebNotification notification)
        {
            await _webNotificationsHttpClient.RemoveWebNotification(notification);
        }

        public async Task<WebNotification> GetNotificationById(int id)
        {
            WebNotification notification = await _webNotificationsHttpClient.GetWebNotificationById(id);

            return notification;
        }

        public async Task<List<WebNotification>> GetUsersNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await _webNotificationsHttpClient.GetUsersWebNotifications(userId);

            return usersNotifications;
        }
    }
}
