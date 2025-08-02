using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing push notifications and devices.
    /// </summary>
    public interface IPushMessageSender
    {
        /// <summary>
        /// Sends a push notification to a user.
        /// </summary>
        /// <param name="userId">The UserId of the user.</param>
        /// <param name="title">The title of the PushNotification to send.</param>
        /// <param name="message">The message/body of the PushNotification.</param>
        /// <param name="link">The link that is navigated to when clicking/tapping the notification.</param>
        /// <param name="tag">The tag property of the PushNotification.</param>
        /// <returns></returns>
        Task SendMessage(string userId, string title, string message, string link, string tag);

        /// <summary>
        /// Gets a PushDevices by Id.
        /// </summary>
        /// <param name="id">The Id of the PushDevices.</param>
        /// <returns>PushDevices object. If the item isn't found or an error occurs a new PushDevices with Id=0 is returned.</returns>
        Task<PushDevices> GetPushDeviceById(int id);

        /// <summary>
        /// Gets a list of all PushDevices.
        /// </summary>
        /// <returns>List of PushDevices objects.</returns>
        Task<List<PushDevices>> GetAllPushDevices();

        /// <summary>
        /// Adds a new PushDevices object to the database.
        /// </summary>
        /// <param name="device">The PushDevices object to add.</param>
        /// <returns>The added PushDevices object.</returns>
        Task<PushDevices> AddPushDevice(PushDevices device);

        /// <summary>
        /// Gets a PushDevices by the PushDevices' Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">The PushDevices object to get.</param>
        /// <returns>PushDevices object with the provided properties. Null if the item isn't found. If an error occurs a new PushDevices object with Id=0.</returns>
        Task<PushDevices> GetDevice(PushDevices device);

        /// <summary>
        /// Removes a PushDevices.
        /// </summary>
        /// <param name="device">The PushDevices object to remove.</param>
        /// <returns>The removed PushDevices object. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
        Task RemoveDevice(PushDevices device);
    }
}
