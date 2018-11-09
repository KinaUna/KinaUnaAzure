using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class NotificationsController : Controller
    {
        public IActionResult ShowNotification(WebNotification notification)
        {
            return PartialView(notification);
        }
    }
}