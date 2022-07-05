using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly WebDbContext _context;

        public WebPushController(WebDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Send(int? id)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            // Todo: Replace with role based access
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
            // Todo: Replace with role based access
            if (userEmail.ToUpper() != Constants.AdminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            StringValues payload = Request.Form["payload"];
            PushDevices device = await _context.PushDevices.SingleOrDefaultAsync(m => m.Id == id);

            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            if (device != null)
            {
                PushSubscription pushSubscription = new PushSubscription(device.PushEndpoint, device.PushP256DH, device.PushAuth);
                VapidDetails vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);

                WebPushClient webPushClient = new WebPushClient();
                webPushClient.SendNotification(pushSubscription, payload, vapidDetails);
            }

            return View();
        }

        public IActionResult GenerateKeys()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            // Todo: Replace with role based access
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