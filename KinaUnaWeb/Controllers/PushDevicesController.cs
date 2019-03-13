using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo
    [Authorize]
    public class PushDevicesController : Controller
    {
        private readonly WebDbContext _context;

        private readonly IConfiguration _configuration;
        
        public PushDevicesController(WebDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Devices
        public async Task<IActionResult> Index()
        {
            return View(await _context.PushDevices.ToListAsync());
        }

        // GET: Devices/Create
        public IActionResult Create()
        {
            ViewBag.PublicKey = _configuration["VapidPublicKey"];

            return View();
        }

        // POST: Devices/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,PushEndpoint,PushP256DH,PushAuth")] PushDevices devices)
        {
            if (ModelState.IsValid)
            {
                string userId = HttpContext.User.FindFirst("sub").Value;
                devices.Name = userId;
                _context.Add(devices);
                await _context.SaveChangesAsync();
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
                string userId = HttpContext.User.FindFirst("sub").Value;
                PushDevices existingDevice = await _context.PushDevices.SingleOrDefaultAsync(p =>
                    p.Name == devices.Name && p.PushP256DH == devices.PushP256DH && p.PushAuth == devices.PushAuth &&
                    p.PushEndpoint == devices.PushEndpoint);
                if (existingDevice == null)
                {
                    devices.Name = userId;
                    _context.Add(devices);
                    await _context.SaveChangesAsync();
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

            var devices = await _context.PushDevices
                .SingleOrDefaultAsync(m => m.Id == id);
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
            var devices = await _context.PushDevices.SingleOrDefaultAsync(m => m.Id == id);
            if (devices != null)
            {
                _context.PushDevices.Remove(devices);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemovePushDevice(PushDevices device)
        {
            // string userId = HttpContext.User.FindFirst("sub").Value;
            PushDevices deleteDevice = await _context.PushDevices.SingleOrDefaultAsync(p => p.Name == device.Name && p.PushP256DH == device.PushP256DH);
            if (deleteDevice != null)
            {
                _context.PushDevices.Remove(deleteDevice);
                await _context.SaveChangesAsync();
            }
           
            return RedirectToAction("MyAccount", "Account");
        }
    }
}