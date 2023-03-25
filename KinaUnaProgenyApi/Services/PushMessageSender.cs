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
    public class PushMessageSender : IPushMessageSender
    {
        private readonly IConfiguration _configuration;
        private readonly IDataService _dataService;

        public PushMessageSender(IConfiguration configuration, IDataService dataService)
        {
            _configuration = configuration;
            _dataService = dataService;
        }

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
            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            List<PushDevices> deviceList = await _dataService.GetPushDevicesListByUserId(user);
            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        await _dataService.RemovePushDevice(dev);
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
                                await _dataService.RemovePushDevice(dev);
                            }
                        }
                    }

                }
            }

        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await _dataService.GetPushDeviceById(id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            var pushDevicesList = await _dataService.GetAllPushDevices();

            return pushDevicesList;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await _dataService.AddPushDevice(device);

            return device;

        }

        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await _dataService.GetPushDevice(device);

            return result;
        }

        public async Task RemoveDevice(PushDevices device)
        {
            await _dataService.RemovePushDevice(device);
        }
    }
}
