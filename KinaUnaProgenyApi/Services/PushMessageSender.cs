using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using WebPush;

namespace KinaUnaProgenyApi.Services
{
    public class PushMessageSender(IConfiguration configuration, IDataService dataService) : IPushMessageSender
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

            List<PushDevices> deviceList = await dataService.GetPushDevicesListByUserId(user);
            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        await dataService.RemovePushDevice(dev);
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
                                await dataService.RemovePushDevice(dev);
                            }
                        }
                    }

                }
            }

        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await dataService.GetPushDeviceById(id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            var pushDevicesList = await dataService.GetAllPushDevices();

            return pushDevicesList;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await dataService.AddPushDevice(device);

            return device;

        }

        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await dataService.GetPushDevice(device);

            return result;
        }

        public async Task RemoveDevice(PushDevices device)
        {
            await dataService.RemovePushDevice(device);
        }
    }
}
