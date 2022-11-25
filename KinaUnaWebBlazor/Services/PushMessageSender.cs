using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWebBlazor.Services
{
    public class PushMessageSender: IPushMessageSender
    {
        private readonly IConfiguration _configuration;

        private readonly WebDbContext _context;

        public PushMessageSender(WebDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task SendMessage(string user, string title, string message, string link, string tag)
        {
            PushNotification notification = new PushNotification();
            notification.Title = title;
            notification.Message = message;
            notification.Link = link;
            notification.Tag = tag;
            string payload = JsonConvert.SerializeObject(notification);
            string vapidPublicKey = _configuration["VapidPublicKey"] ?? throw new InvalidOperationException("VapidPublicKey value missing in configuration");
            string vapidPrivateKey = _configuration["VapidPrivateKey"] ?? throw new InvalidOperationException("VapidPrivateKey value missing in configuration");

            List<PushDevices> deviceList = await _context.PushDevices.Where(m => m.Name == user).ToListAsync();
            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new PushSubscription(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (String.IsNullOrEmpty(dev.PushAuth) || String.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        _context.PushDevices.Remove(dev);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        WebPushClient webPushClient = new WebPushClient();
                        try
                        {
                            webPushClient.SendNotification(pushSubscription, payload, vapidDetails);
                        }
                        catch (WebPushException ex)
                        {
                            if (ex.Message == "Subscription no longer valid")
                            {
                                _context.PushDevices.Remove(dev);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    
                }
            }
            
        }
    }
}
