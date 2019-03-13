using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

            var payload = Request.Form["payload"];
            var device = await _context.PushDevices.SingleOrDefaultAsync(m => m.Id == id);

            string vapidPublicKey = _configuration["VapidPublicKey"];
            string vapidPrivateKey = _configuration["VapidPrivateKey"];

            var pushSubscription = new PushSubscription(device.PushEndpoint, device.PushP256DH, device.PushAuth);
            var vapidDetails = new VapidDetails("mailto:" + Constants.SupportEmail, vapidPublicKey, vapidPrivateKey);

            var webPushClient = new WebPushClient();
            webPushClient.SendNotification(pushSubscription, payload, vapidDetails);

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
            var keys = VapidHelper.GenerateVapidKeys();
            ViewBag.PublicKey = keys.PublicKey;
            ViewBag.PrivateKey = keys.PrivateKey;
            return View();
        }
    }
}