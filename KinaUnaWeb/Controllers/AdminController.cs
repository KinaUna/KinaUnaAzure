using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KinaUnaWeb.Controllers
{
    public class AdminController: Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly WebDbContext _context;
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly string _adminEmail = Constants.AdminEmail;

        public AdminController(IProgenyHttpClient progenyHttpClient, WebDbContext context,
            IBackgroundTaskQueue queue, IHubContext<WebNotificationHub> hubContext, IPushMessageSender pushMessageSender)
        {
            _progenyHttpClient = progenyHttpClient;
            _context = context;
            Queue = queue;
            _hubContext = hubContext;
            _pushMessageSender = pushMessageSender;
        }

        private IBackgroundTaskQueue Queue { get; }

        public IActionResult Index()
        {
            // Todo: Implement Admin as role instead
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? Constants.DefaultUserEmail;
            
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        
        public IActionResult SendAdminMessage()
        {
            // Todo: Implement Admin as role instead
            WebNotification model = new WebNotification();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendAdminMessage(WebNotification notification)
        {
            // Todo: Implement Admin as role instead
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = User.FindFirst("timezone")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            if (userEmail.ToUpper() == _adminEmail.ToUpper())
            {
                if (notification.To == "OnlineUsers")
                {
                    notification.DateTime = DateTime.UtcNow;
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
                }
                else
                {
                    UserInfo userinfo;
                    if (notification.To.Contains('@'))
                    {
                        userinfo = await _progenyHttpClient.GetUserInfo(notification.To);
                        notification.To = userinfo.UserId;
                    }
                    else
                    {
                        userinfo = await _progenyHttpClient.GetUserInfoByUserId(notification.To);
                    }

                    notification.DateTime = DateTime.UtcNow;
                    await _context.WebNotificationsDb.AddAsync(notification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userinfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));

                    WebNotification webNotification = new WebNotification();
                    webNotification.Title = "Notification Sent" ;
                    webNotification.Message = "To: " + notification.To + "<br/>From: " + notification.From + "<br/><br/>Message: <br/>" + notification.Message;
                    webNotification.From = Constants.AppName + " Notification System";
                    webNotification.Type = "Notification";
                    webNotification.DateTime = DateTime.UtcNow;
                    webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
            }

            notification.Title = "Notification Added";
            return View(notification);
        }

        public IActionResult SendPush()
        {
            // Todo: Implement Admin as role instead
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            PushNotification notification = new PushNotification();
            notification.UserId = userId;
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> SendPush(PushNotification notification)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            if (notification.UserId.Contains('@'))
            {
                UserInfo userinfo = await _progenyHttpClient.GetUserInfo(notification.UserId);
                notification.UserId = userinfo.UserId;
            }

            await _pushMessageSender.SendMessage(notification.UserId, notification.Title, notification.Message,
                notification.Link, "kinaunapush");
            notification.Title = "Message Sent";
            return View(notification);
        }
    }
}
