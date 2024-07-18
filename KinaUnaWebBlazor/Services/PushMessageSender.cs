using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWebBlazor.Services
{
    public class PushMessageSender(ProgenyDbContext context, IConfiguration configuration) : IPushMessageSender
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
            string vapidPublicKey = configuration["VapidPublicKey"] ?? throw new InvalidOperationException("VapidPublicKey value missing in configuration");
            string vapidPrivateKey = configuration["VapidPrivateKey"] ?? throw new InvalidOperationException("VapidPrivateKey value missing in configuration");

            List<PushDevices> deviceList = await context.PushDevices.Where(m => m.Name == user).ToListAsync();
            if (deviceList.Count != 0)
            {
                foreach (PushDevices dev in deviceList)
                {
                    PushSubscription pushSubscription = new(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (string.IsNullOrEmpty(dev.PushAuth) || string.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        context.PushDevices.Remove(dev);
                        await context.SaveChangesAsync();
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
                                context.PushDevices.Remove(dev);
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                    
                }
            }
            
        }
    }
}
