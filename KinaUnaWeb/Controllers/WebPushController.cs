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
    public class WebPushController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IPushMessageSender _messageSender;

        public WebPushController(IConfiguration configuration, IPushMessageSender pushMessageSender)
        {
            _configuration = configuration;
            _messageSender = pushMessageSender;
        }

        public IActionResult Send()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (userEmail.ToUpper() != Constants.AdminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost, ActionName("Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (userEmail.ToUpper() != Constants.AdminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            StringValues payload = Request.Form["payload"];
            PushDevices device = await _messageSender.GetPushDeviceById(id);

            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            if (device != null)
            {
                PushSubscription pushSubscription = new PushSubscription(device.PushEndpoint, device.PushP256DH, device.PushAuth);
                VapidDetails vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);

                WebPushClient webPushClient = new WebPushClient();
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }

            return View();
        }

        public IActionResult GenerateKeys()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            
            if (userEmail.ToUpper() != Constants.AdminEmail.ToUpper())
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