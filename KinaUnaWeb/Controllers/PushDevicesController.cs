using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo
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

        
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,PushEndpoint,PushP256DH,PushAuth")] PushDevices devices)
        {
            if (ModelState.IsValid)
            {
                string userId = HttpContext.User.FindFirst("sub")?.Value;
                devices.Name = userId;
                _ = await messageSender.AddPushDevice(devices);

                return RedirectToAction(nameof(Index));
            }

            return View(devices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind("Id,Name,PushEndpoint,PushP256DH,PushAuth")] PushDevices devices)
        {
            if (ModelState.IsValid)
            {
                string userId = HttpContext.User.FindFirst("sub")?.Value;
                PushDevices existingDevice = await messageSender.GetDevice(devices);
                if (existingDevice == null)
                {
                    devices.Name = userId;
                    _ = await messageSender.AddPushDevice(devices);
                }

                return RedirectToAction("MyAccount", "Account");
            }

            return RedirectToAction("EnablePush", "Account");
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