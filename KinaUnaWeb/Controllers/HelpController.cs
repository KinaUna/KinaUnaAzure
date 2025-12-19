using System.Threading.Tasks;
using KinaUna.Data.Models.Support;
using KinaUnaWeb.Services.HttpClients.Support;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class HelpController(IHelpHttpClient helpHttpClient) : Controller
    {
        public async Task<IActionResult> HelpDetails(string page, string element, int languageId)
        {
            HelpContent helpContent = await helpHttpClient.GetHelpContent(page, element, languageId);
            return PartialView("HelpContentPartialView", helpContent);
        }
    }
}
