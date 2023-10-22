using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers;

public class StartController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}