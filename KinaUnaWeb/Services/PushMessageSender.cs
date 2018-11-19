using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public async Task SendMessage(string user, string title, string message, string link)
        {
            var payload = "{{\"title\":\"" + title + "\", \"message\":\"" + message + "\"}}";
            var deviceList = await _context.PushDevices.Where(m => m.Name == user).ToListAsync();

            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            if (deviceList.Any())
            {
                foreach (PushDevices dev in deviceList)
                {
                    var pushSubscription = new PushSubscription(dev.PushEndpoint, dev.PushP256DH, dev.PushAuth);
                    var vapidDetails = new VapidDetails("mailto:support@kinauna.com", vapidPublicKey, vapidPrivateKey);

                    var webPushClient = new WebPushClient();
                    try
                    {
                        webPushClient.SendNotification(pushSubscription, title, vapidDetails);
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
