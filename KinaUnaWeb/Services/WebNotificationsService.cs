using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing WebNotifications.
    /// </summary>
    /// <param name="webNotificationsHttpClient"></param>
    public class WebNotificationsService(IWebNotificationsHttpClient webNotificationsHttpClient) : IWebNotificationsService
    {
        /// <summary>
        /// Saves a WebNotification to the database.
        /// </summary>
        /// <param name="notification">The WebNotification to save.</param>
        /// <returns>The saved WebNotification object.</returns>
        public async Task<WebNotification> SaveNotification(WebNotification notification)
        {
            notification = await webNotificationsHttpClient.AddWebNotification(notification);

            return notification;
        }

        /// <summary>
        /// Updates a WebNotification in the database.
        /// </summary>
        /// <param name="notification">The WebNotification with the updated properties.</param>
        /// <returns>The updated WebNotification.</returns>
        public async Task<WebNotification> UpdateNotification(WebNotification notification)
        {
            notification = await webNotificationsHttpClient.UpdateWebNotification(notification);

            return notification;
        }

        /// <summary>
        /// Removes a WebNotification from the database.
        /// </summary>
        /// <param name="notification">The WebNotification to remove.</param>
        /// <returns></returns>
        public async Task RemoveNotification(WebNotification notification)
        {
            await webNotificationsHttpClient.RemoveWebNotification(notification);
        }

        /// <summary>
        /// Gets a WebNotification by Id.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. If the item cannot be found a new WebNotification with Id=0 is returned.</returns>
        public async Task<WebNotification> GetNotificationById(int id)
        {
            WebNotification notification = await webNotificationsHttpClient.GetWebNotificationById(id);

            return notification;
        }

        /// <summary>
        /// Gets a list of all WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user to get WebNotifications for.</param>
        /// <returns>List of WebNotification objects.</returns>
        public async Task<List<WebNotification>> GetUsersNotifications(string userId)
        {
            List<WebNotification> usersNotifications = await webNotificationsHttpClient.GetUsersWebNotifications(userId);

            return usersNotifications;
        }

        /// <summary>
        /// Gets a list of the latest WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <param name="start">Number of WebNotifications to skip.</param>
        /// <param name="count">Number of WebNotifications to include.</param>
        /// <param name="unreadOnly">Include only unread WebNotifications.</param>
        /// <returns>List of WebNotification objects.</returns>
        public async Task<List<WebNotification>> GetLatestNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true)
        {
            List<WebNotification> latestNotifications = await webNotificationsHttpClient.GetLatestWebNotifications(userId, start, count, unreadOnly);

            return latestNotifications;
        }

        /// <summary>
        /// Gets the number of WebNotifications for a user, including both read and unread ones.
        /// </summary>
        /// <param name="userId">The UserId of the user to get the count for.</param>
        /// <returns>Integer with the count of WebNotifications.</returns>
        public async Task<int> GetUsersNotificationsCount(string userId)
        {
            int notificationsCount = await webNotificationsHttpClient.GetUsersNotificationsCount(userId);

            return notificationsCount;
        }
    }
}
