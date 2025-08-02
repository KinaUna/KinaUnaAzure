using System.Threading.Tasks;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo

    /// <summary>
    /// Controller for managing (Web) Push Devices.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="messageSender"></param>
    [Authorize]
    public class PushDevicesController(IConfiguration configuration, IPushMessageSender messageSender) : Controller
    {
        public async Task<IActionResult> Index()
        {
            return View(await messageSender.GetAllPushDevices());
        }

        public IActionResult Create()
        {
            ViewBag.PublicKey = configuration["VapidPublicKey"];

            return View();
        }

        
        // To protect from over posting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,PushEndpoint,PushP256DH,PushAuth")] PushDevices devices)
        {
            if (!ModelState.IsValid) return View(devices);

            string userId = HttpContext.User.FindFirst("sub")?.Value;
            devices.Name = userId;
            _ = await messageSender.AddPushDevice(devices);

            return RedirectToAction(nameof(Index));

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("Id,Name,PushEndpoint,PushP256DH,PushAuth")] PushDevices devices)
        {
            if (!ModelState.IsValid) return RedirectToAction("EnablePush", "Account");
            
            PushDevices existingDevice = await messageSender.GetDevice(devices);
            if (existingDevice != null) return RedirectToAction("MyAccount", "Account");

            string userId = HttpContext.User.FindFirst("sub")?.Value;
            devices.Name = userId;
            _ = await messageSender.AddPushDevice(devices);

            return RedirectToAction("MyAccount", "Account");

        }

        // GET: Devices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            PushDevices devices = await messageSender.GetPushDeviceById(id.Value);

            if (devices == null)
            {
                return NotFound();
            }

            return View(devices);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            PushDevices devices = await messageSender.GetPushDeviceById(id);
            
            if (devices != null)
            {
                await messageSender.RemoveDevice(devices);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemovePushDevice(PushDevices device)
        {
            // string userId = HttpContext.User.FindFirst("sub").Value;
            PushDevices deleteDevice = await messageSender.GetDevice(device);
            if (deleteDevice != null)
            {
                await messageSender.RemoveDevice(deleteDevice);
            }
           
            return RedirectToAction("MyAccount", "Account");
        }
    }
}