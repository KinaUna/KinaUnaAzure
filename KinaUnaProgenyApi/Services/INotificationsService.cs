using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface INotificationsService
    {
        /// <summary>
        /// Gets a MobileNotification by NotificationId.
        /// </summary>
        /// <param name="id">The NotificationId of the Notification to get.</param>
        /// <returns>The Notification with the given NotificationId. Null if it doesn't exist.</returns>
        Task<MobileNotification> GetMobileNotification(int id);

        /// <summary>
        /// Adds a new MobileNotification to the database.
        /// </summary>
        /// <param name="notification">The MobileNotification to add.</param>
        /// <returns>The added MobileNotification.</returns>
        Task<MobileNotification> AddMobileNotification(MobileNotification notification);

        /// <summary>
        /// Updates a MobileNotification in the database.
        /// </summary>
        /// <param name="notification">The MobileNotification with the updated properties.</param>
        /// <returns>The updated MobileNotification.</returns>
        Task<MobileNotification> UpdateMobileNotification(MobileNotification notification);

        /// <summary>
        /// Deletes a MobileNotification from the database.
        /// </summary>
        /// <param name="notification">The MobileNotification to delete.</param>
        /// <returns>The deleted Notification.</returns>
        Task<MobileNotification> DeleteMobileNotification(MobileNotification notification);

        /// <summary>
        /// Gets a list of all MobileNotifications for a user in a specific language.
        /// </summary>
        /// <param name="userId">The UserId of the user to get Notifications for.</param>
        /// <param name="language">The Language of the Notifications.</param>
        /// <returns>List of Notifications.</returns>
        Task<List<MobileNotification>> GetUsersMobileNotifications(string userId, string language);

        /// <summary>
        /// Adds a new PushDevice to the database.
        /// </summary>
        /// <param name="device">The PushDevice to add.</param>
        /// <returns>The added PushDevice.</returns>
        Task<PushDevices> AddPushDevice(PushDevices device);

        /// <summary>
        /// Deletes a PushDevice from the database.
        /// </summary>
        /// <param name="device">The PushDevice to remove.</param>
        /// <returns></returns>
        Task RemovePushDevice(PushDevices device);

        /// <summary>
        /// Gets a PushDevice by Id.
        /// </summary>
        /// <param name="id">The Id of the PushDevice to get.</param>
        /// <returns>The PushDevice with the given Id. Null if it doesn't exist.</returns>
        Task<PushDevices> GetPushDeviceById(int id);

        /// <summary>
        /// Gets a list of all PushDevices in the database.
        /// </summary>
        /// <returns>List of all PushDevices.</returns>
        Task<List<PushDevices>> GetAllPushDevices();

        /// <summary>
        /// Gets a PushDevice by the PushDevice's Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">The PushDevice to get.</param>
        /// <returns>The PushDevice.</returns>
        Task<PushDevices> GetPushDevice(PushDevices device);

        /// <summary>
        /// Gets a list of all PushDevices for a user.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>The PushDevice if found. Null if it doesn't exist.</returns>
        Task<List<PushDevices>> GetPushDevicesListByUserId(string userId);

        /// <summary>
        /// Adds a new WebNotification to the database and sets it in the cache.
        /// </summary>
        /// <param name="notification">The WebNotification to add.</param>
        /// <returns>The added WebNotification.</returns>
        Task<WebNotification> AddWebNotification(WebNotification notification);

        /// <summary>
        /// Updates a WebNotification in the database and sets it in the cache.
        /// </summary>
        /// <param name="notification">The WebNotification with updated properties.</param>
        /// <returns>The updated WebNotification.</returns>
        Task<WebNotification> UpdateWebNotification(WebNotification notification);

        /// <summary>
        /// Removes a WebNotification from the database and the cache.
        /// </summary>
        /// <param name="notification">The WebNotification to remove.</param>
        /// <returns></returns>
        Task RemoveWebNotification(WebNotification notification);

        /// <summary>
        /// Gets a WebNotification by Id.
        /// </summary>
        /// <param name="id">The Id of the WebNotification to get.</param>
        /// <returns>The WebNotification with the given Id. Null if the WebNotification doesn't exist.</returns>
        Task<WebNotification> GetWebNotificationById(int id);

        /// <summary>
        /// Gets a list of all WebNotifications for a user from the cache.
        /// </summary>
        /// <param name="userId">The UserId of the user to get all WebNotifications for.</param>
        /// <returns></returns>
        Task<List<WebNotification>> GetUsersWebNotifications(string userId);

        /// <summary>
        /// Gets a list of the latest WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user to get Notifications for.</param>
        /// <param name="start">Number of WebNotifications to skip.</param>
        /// <param name="count">Number of WebNotifications to get.</param>
        /// <param name="unreadOnly">Filter the list, if unreadOnly is true only include the WebNotification with IsRead set to false.</param>
        /// <returns>List of WebNotifications.</returns>
        Task<List<WebNotification>> GetLatestWebNotifications(string userId, int start, int count, bool unreadOnly);

        /// <summary>
        /// Gets the number of WebNotifications for a user.
        /// </summary>
        /// <param name="userId">The UserId of the user.</param>
        /// <returns>Number of WebNotifications found for the user.</returns>
        Task<int> GetUsersNotificationsCount(string userId);
    }
}
