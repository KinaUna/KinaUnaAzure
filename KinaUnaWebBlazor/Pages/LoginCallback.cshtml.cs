using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KinaUnaWebBlazor.Pages
{
    public class LoginCallbackModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Redirect("/");
        }
    }
}
