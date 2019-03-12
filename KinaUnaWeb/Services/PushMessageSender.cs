using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.IDP;
using KinaUnaWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebPush;

namespace KinaUnaWeb.Services
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
            var payload = JsonConvert.SerializeObject(notification);
            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            var deviceList = await _context.PushDevices.Where(m => m.Name == user).ToListAsync();
            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    var pushSubscription = new PushSubscription(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    var vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);
                    if (String.IsNullOrEmpty(dev.PushAuth) || String.IsNullOrEmpty(dev.PushEndpoint))
                    {
                        _context.PushDevices.Remove(dev);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var webPushClient = new WebPushClient();
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
