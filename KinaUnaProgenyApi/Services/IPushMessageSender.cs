using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services;

public interface IPushMessageSender
{
    /// <summary>
    /// Sends push notifications to all devices registered to a user.
    /// If the device is no longer valid, it is removed from the database.
    /// </summary>
    /// <param name="user">The UserId of the User to send notifications to.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message/body of the notification.</param>
    /// <param name="link">The link/action when the notification is clicked.</param>
    /// <param name="tag">The notification tag.</param>
    /// <returns></returns>
    Task SendMessage(string user, string title, string message, string link, string tag);

    /// <summary>
    /// Gets a PushDevice by Id.
    /// </summary>
    /// <param name="id">The PushDevice Id.</param>
    /// <returns>The PushDevice with the given Id.</returns>
    Task<PushDevices> GetPushDeviceById(int id);

    /// <summary>
    /// Gets a list of all PushDevices in the database.
    /// </summary>
    /// <returns>List of PushDevice objects.</returns>
    Task<List<PushDevices>> GetAllPushDevices();

    /// <summary>
    /// Adds a new PushDevice to the database.
    /// </summary>
    /// <param name="device">PushDevice object to add.</param>
    /// <returns>The added PushDevice.</returns>
    Task<PushDevices> AddPushDevice(PushDevices device);

    /// <summary>
    /// Gets a PushDevice by the PushDevice's Name, PushP256DH, PushAuth, and PushEndPoint properties.
    /// </summary>
    /// <param name="device">PushDevice object with the properties to find.</param>
    /// <returns>PushDevice.</returns>
    Task<PushDevices> GetDevice(PushDevices device);

    /// <summary>
    /// Removes a PushDevice from the database.
    /// </summary>
    /// <param name="device">The PushDevice to remove.</param>
    /// <returns></returns>
    Task RemoveDevice(PushDevices device);
}