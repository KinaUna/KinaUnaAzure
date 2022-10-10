using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KinaUnaWebBlazor.Pages
{
    public class LogInModel : PageModel
    {
        public async Task OnGet(string redirectUri)
        {
            if (HttpContext.User.Identity != null && !HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new
                    AuthenticationProperties
                    { RedirectUri = redirectUri });
            }
            else
            {
                // redirect to the root
                Response.Redirect("/");
            }
            
        }
    }
}
