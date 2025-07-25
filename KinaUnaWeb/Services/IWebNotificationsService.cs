using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing WebNotifications.
    /// </summary>
    public interface IWebNotificationsService
    {
        /// <summary>
        /// Saves a WebNotification to the database.
        /// </summary>
        /// <param name="notification">The WebNotification to save.</param>
        /// <returns>The saved WebNotification object.</returns>
        Task<WebNotification> SaveNotification(WebNotification notification);

        /// <summary>
        /// Updates a WebNotification in the database.
        /// </summary>
        /// <param name="notification">The WebNotification with the updated properties.</param>
        /// <returns>The updated WebNotification.</returns>
        Task<WebNotification> UpdateNotification(WebNotification notification);

        /// <summary>
        /// Removes a WebNotification from the database.
        /// </summary>
        /// <param name="notification">The WebNotification to remove.</param>
        /// <returns></returns>
        Task RemoveNotification(WebNotification notification);

        /// <summary>
        /// Gets a WebNotification by Id.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. If the item cannot be found a new WebNotification with Id=0 is returned.</returns>
        Task<WebNotification> GetNotificationById(int id);

        /// <summary>
        /// Gets a list of all WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user to get WebNotifications for.</param>
        /// <returns>List of WebNotification objects.</returns>
        Task<List<WebNotification>> GetUsersNotifications(string userId);

        /// <summary>
        /// Gets a list of the latest WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <param name="start">Number of WebNotifications to skip.</param>
        /// <param name="count">Number of WebNotifications to include.</param>
        /// <param name="unreadOnly">Include only unread WebNotifications.</param>
        /// <returns>List of WebNotification objects.</returns>
        Task<List<WebNotification>> GetLatestNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true);

        /// <summary>
        /// Gets the number of WebNotifications for a user, including both read and unread ones.
        /// </summary>
        /// <param name="userId">The UserId of the user to get the count for.</param>
        /// <returns>Integer with the count of WebNotifications.</returns>
        Task<int> GetUsersNotificationsCount(string userId);
    }
}
