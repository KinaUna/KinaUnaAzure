using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWeb.Services
{
    public class PushMessageSender(IConfiguration configuration, IWebNotificationsHttpClient webNotificationHttpClient) : IPushMessageSender
    {
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

            List<PushDevices> deviceList = await webNotificationHttpClient.GetPushDevicesListByUserId(user);
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

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await webNotificationHttpClient.GetPushDeviceById(id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            List<PushDevices> pushDevicesList = await webNotificationHttpClient.GetAllPushDevices();

            return pushDevicesList;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await webNotificationHttpClient.AddPushDevice(device);

            return device;

        }

        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await webNotificationHttpClient.GetPushDevice(device);
            
            return result;
        }

        public async Task RemoveDevice(PushDevices device)
        {
            await webNotificationHttpClient.RemovePushDevice(device);
        }
    }
}
