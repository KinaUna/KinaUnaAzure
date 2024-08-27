using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using WebPush;

namespace KinaUnaWeb.Controllers
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo
    
    [Authorize]
    public class WebPushController(IConfiguration configuration, IPushMessageSender pushMessageSender) : Controller
    {
        private readonly string _adminEmail = configuration.GetValue<string>("AdminEmail");

        /// <summary>
        /// Page for sending a push message.
        /// Only available for the Admin user.
        /// </summary>
        /// <returns>View.</returns>
        public IActionResult Send()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (!userEmail.Equals(_adminEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        /// <summary>
        /// HttpPost action for sending a push message.
        /// </summary>
        /// <param name="id">The UserId of the user to send a message to.</param>
        /// <returns>View.</returns>
        [HttpPost, ActionName("Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (!userEmail.Equals(_adminEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            StringValues payload = Request.Form["payload"];
            PushDevices device = await pushMessageSender.GetPushDeviceById(id);

            string vapidPublicKey = configuration["VapidPublicKey"];
            string vapidPrivateKey = configuration["VapidPrivateKey"];

            if (device == null) return View();

            PushSubscription pushSubscription = new(device.PushEndpoint, device.PushP256DH, device.PushAuth);
            VapidDetails vapidDetails = new("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);

            WebPushClient webPushClient = new();
            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);

            return View();
        }

        /// <summary>
        /// Page for generating VAPID keys.
        /// For the Admin user only.
        /// Should only be used once, and the keys should be stored in the configuration file, environment variable, or Azure Key Vault.
        /// </summary>
        /// <returns>View.</returns>
        public IActionResult GenerateKeys()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (!userEmail.Equals(_adminEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }
            VapidDetails keys = VapidHelper.GenerateVapidKeys();
            ViewBag.PublicKey = keys.PublicKey;
            ViewBag.PrivateKey = keys.PrivateKey;
            return View();
        }
    }
}