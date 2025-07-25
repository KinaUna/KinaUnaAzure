using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients;

public interface IWebNotificationsHttpClient
{
    /// <summary>
    /// Gets all PushDevices.
    /// </summary>
    /// <returns>List of PushDevices objects.</returns>
    Task<List<PushDevices>> GetAllPushDevices();

    /// <summary>
    /// Gets a PushDevices by Id.
    /// </summary>
    /// <param name="id">The Id of the PushDevices to get.</param>
    /// <returns>The PushDevices object with the given Id. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
    Task<PushDevices> GetPushDeviceById(int id);

    /// <summary>
    /// Adds a new PushDevices.
    /// </summary>
    /// <param name="device">The PushDevices to add.</param>
    /// <returns>The added PushDevices object. If an error occurs, return a new PushDevices with Id=0.</returns>
    Task<PushDevices> AddPushDevice(PushDevices device);

    /// <summary>
    /// Removes a PushDevices.
    /// </summary>
    /// <param name="device">The PushDevices object to remove.</param>
    /// <returns>The removed PushDevices object. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
    Task<PushDevices> RemovePushDevice(PushDevices device);

    /// <summary>
    /// Get the list of all PushDevices for a given user.
    /// </summary>
    /// <param name="userId">The user's UserId.</param>
    /// <returns>List of PushDevices objects.</returns>
    Task<List<PushDevices>> GetPushDevicesListByUserId(string userId);

    /// <summary>
    /// Gets a PushDevices by the PushDevices' Name, PushP256DH, PushAuth, and PushEndPoint properties.
    /// </summary>
    /// <param name="device">The PushDevices object to get.</param>
    /// <returns>PushDevices object with the provided properties. Null if the item isn't found. If an error occurs a new PushDevices object with Id=0.</returns>
    Task<PushDevices> GetPushDevice(PushDevices device);

    /// <summary>
    /// Adds a new WebNotification.
    /// </summary>
    /// <param name="notification">The WebNotification to add.</param>
    /// <returns>The added WebNotification object. If an error occurs a new WebNotification with Id=0 is returned.</returns>
    Task<WebNotification> AddWebNotification(WebNotification notification);

    /// <summary>
    /// Updates a WebNotification.
    /// </summary>
    /// <param name="notification">The WebNotification with the updated properties.</param>
    /// <returns>The updated WebNotification. If not found or an error occurs, a new WebNotification with Id=0 is returned.</returns>
    Task<WebNotification> UpdateWebNotification(WebNotification notification);

    /// <summary>
    /// Removes a WebNotification.
    /// </summary>
    /// <param name="notification">The WebNotification to remove.</param>
    /// <returns>The deleted WebNotification. If not found or an error occurs, a new WebNotification with Id=0 is returned.</returns>
    Task<WebNotification> RemoveWebNotification(WebNotification notification);

    /// <summary>
    /// Gets a WebNotification by Id.
    /// </summary>
    /// <param name="id">The Id of the WebNotification to get.</param>
    /// <returns>The WebNotification with the given Id. If the item cannot be found or an error occurs a new WebNotification with Id=0 is returned.</returns>
    Task<WebNotification> GetWebNotificationById(int id);

    /// <summary>
    /// Gets the list of all WebNotifications for a given user.
    /// </summary>
    /// <param name="userId">The user's UserId.</param>
    /// <returns>List of WebNotification objects.</returns>
    Task<List<WebNotification>> GetUsersWebNotifications(string userId);

    /// <summary>
    /// Gets the latest WebNotifications for a given user.
    /// </summary>
    /// <param name="userId">The user's UserId.</param>
    /// <param name="start">Number of WebNotifications to skip.</param>
    /// <param name="count">Number of WebNotifications to get.</param>
    /// <param name="unreadOnly">Include unread WebNotifications only.</param>
    /// <returns>List of WebNotification objects.</returns>
    Task<List<WebNotification>> GetLatestWebNotifications(string userId, int start = 0, int count = 10, bool unreadOnly = true);

    /// <summary>
    /// Gets the number of WebNotifications for a given user, including both read and unread.
    /// </summary>
    /// <param name="userId">The user's UserId.</param>
    /// <returns>Integer with the number of WebNotifications.</returns>
    Task<int> GetUsersNotificationsCount(string userId);
}