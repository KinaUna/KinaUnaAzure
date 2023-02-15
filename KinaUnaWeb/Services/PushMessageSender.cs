using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWeb.Services
{
    public class PushMessageSender: IPushMessageSender
    {
        private readonly IConfiguration _configuration;
        private readonly IWebNotificationsHttpClient _webNotificationHttpClient;
        

        public PushMessageSender(IConfiguration configuration, IWebNotificationsHttpClient webNotificationHttpClient)
        {
            _configuration = configuration;
            _webNotificationHttpClient = webNotificationHttpClient;
        }

        public async Task SendMessage(string user, string title, string message, string link, string tag)
        {
            PushNotification notification = new PushNotification();
            notification.Title = title;
            notification.Message = message;
            notification.Link = link;
            notification.Tag = tag;
            string payload = JsonConvert.SerializeObject(notification);
            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            List<PushDevices> deviceList = await _webNotificationHttpClient.GetPushDevicesListByUserId(user);
            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new PushSubscription(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        await _webNotificationHttpClient.RemovePushDevice(dev);
                    }
                    else
                    {
                        WebPushClient webPushClient = new WebPushClient();
                        try
                        {
                            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                        }
                        catch (WebPushException ex)
                        {
                            if (ex.Message == "Subscription no longer valid")
                            {
                                await _webNotificationHttpClient.RemovePushDevice(dev);
                            }
                        }
                    }
                    
                }
            }
            
        }

        public async Task<PushDevices> GetPushDeviceById(int id)
        {
            PushDevices device = await _webNotificationHttpClient.GetPushDeviceById(id);

            return device;
        }

        public async Task<List<PushDevices>> GetAllPushDevices()
        {
            var pushDevicesList = await _webNotificationHttpClient.GetAllPushDevices();

            return pushDevicesList;
        }

        public async Task<PushDevices> AddPushDevice(PushDevices device)
        {
            device = await _webNotificationHttpClient.AddPushDevice(device);

            return device;

        }

        public async Task<PushDevices> GetDevice(PushDevices device)
        {
            PushDevices result = await _webNotificationHttpClient.GetPushDevice(device);
            
            return result;
        }

        public async Task RemoveDevice(PushDevices device)
        {
            await _webNotificationHttpClient.RemovePushDevice(device);
        }
    }
}
