﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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