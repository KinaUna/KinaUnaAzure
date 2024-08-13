using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using WebPush;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// PushMessageSender service for PWA push notifications.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="notificationsService"></param>
    public class PushMessageSender(IConfiguration configuration, INotificationsService notificationsService) : IPushMessageSender
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
        public async Task SendMessage(string user, string title, string message, string link, string tag)
        {
            PushNotification notification = new()
            {
                Title = title,
                Message = message,
                Link = link,
                Tag = tag
            };
            string payload = JsonConvert.SerializeObject(notification);
            string vapidPublicKey = configuration["VapidPublicKey"];
            string vapidPrivateKey = configuration["VapidPrivateKey"];

            List<PushDevices> deviceList = await notificationsService.GetPushDevicesListByUserId(user);
            if (deviceList.Count != 0)
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        await notificationsService.RemovePushDevice(dev);
                    }
                    else
                    {
                        WebPushClient webPushClient = new();
                        try
                        {
                            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                        }
                        catch (WebPushException ex)
                        {
                            if (ex.Message == "Subscription no longer valid")
                            {
                                await notificationsService.RemovePushDevice(dev);
                            }
                        }
                    }

                }
            }

        }

        /// <summary>
        /// Gets a PushDevice by Id.
        /// </summary>
        /// <param name="id">The PushDevice Id.</param>
        /// <returns>The PushDevice with the given Id.</returns>
        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await notificationsService.GetPushDeviceById(id);

            return device;
        }

        /// <summary>
        /// Gets a list of all PushDevices in the database.
        /// </summary>
        /// <returns>List of PushDevice objects.</returns>
        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = await notificationsService.GetAllPushDevices();

            return pushDevicesList;
        }

        /// <summary>
        /// Adds a new PushDevice to the database.
        /// </summary>
        /// <param name="device">PushDevice object to add.</param>
        /// <returns>The added PushDevice.</returns>
        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await notificationsService.AddPushDevice(device);

            return device;

        }

        /// <summary>
        /// Gets a PushDevice by the PushDevice's Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">PushDevice object with the properties to find.</param>
        /// <returns>PushDevice.</returns>
        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await notificationsService.GetPushDevice(device);

            return result;
        }

        /// <summary>
        /// Removes a PushDevice from the database.
        /// </summary>
        /// <param name="device">The PushDevice to remove.</param>
        /// <returns></returns>
        public async Task RemoveDevice(PushDevices device)
        {
            await notificationsService.RemovePushDevice(device);
        }
    }
}
