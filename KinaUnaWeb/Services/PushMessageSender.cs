using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing push notifications and devices.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="webNotificationHttpClient"></param>
    public class PushMessageSender(IConfiguration configuration, IWebNotificationsHttpClient webNotificationHttpClient) : IPushMessageSender
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
        public async Task SendMessage(string userId, string title, string message, string link, string tag)
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

            List<PushDevices> deviceList = await webNotificationHttpClient.GetPushDevicesListByUserId(userId);
            if (deviceList.Count != 0)
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        await webNotificationHttpClient.RemovePushDevice(dev);
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
                                await webNotificationHttpClient.RemovePushDevice(dev);
                            }
                        }
                    }
                    
                }
            }
            
        }

        /// <summary>
        /// Gets a PushDevices by Id.
        /// </summary>
        /// <param name="id">The Id of the PushDevices.</param>
        /// <returns>PushDevices object. If the item isn't found or an error occurs a new PushDevices with Id=0 is returned.</returns>
        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await webNotificationHttpClient.GetPushDeviceById(id);

            return device;
        }

        /// <summary>
        /// Gets a list of all PushDevices.
        /// </summary>
        /// <returns>List of PushDevices objects.</returns>
        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = await webNotificationHttpClient.GetAllPushDevices();

            return pushDevicesList;
        }

        /// <summary>
        /// Adds a new PushDevices object to the database.
        /// </summary>
        /// <param name="device">The PushDevices object to add.</param>
        /// <returns>The added PushDevices object.</returns>
        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await webNotificationHttpClient.AddPushDevice(device);

            return device;

        }

        /// <summary>
        /// Gets a PushDevices by the PushDevices' Name, PushP256DH, PushAuth, and PushEndPoint properties.
        /// </summary>
        /// <param name="device">The PushDevices object to get.</param>
        /// <returns>PushDevices object with the provided properties. Null if the item isn't found. If an error occurs a new PushDevices object with Id=0.</returns>
        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await webNotificationHttpClient.GetPushDevice(device);
            
            return result;
        }

        /// <summary>
        /// Removes a PushDevices.
        /// </summary>
        /// <param name="device">The PushDevices object to remove.</param>
        /// <returns>The removed PushDevices object. If not found or an error occurs, return a new PushDevices with Id=0.</returns>
        public async Task RemoveDevice(PushDevices device)
        {
            await webNotificationHttpClient.RemovePushDevice(device);
        }
    }
}
