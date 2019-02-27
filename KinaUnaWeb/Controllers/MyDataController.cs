using System;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class MyDataController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ChildSpreadSheet()
        {
            // Todo: Implement spreadsheet functions.
            throw new NotImplementedException();
        }
    }
}