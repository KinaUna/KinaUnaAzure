using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class FamilyController : Controller
    {
        
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public FamilyController(IProgenyHttpClient progenyHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
        }

        public async Task<IActionResult> Index()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            Family myFamily = new Family();
            myFamily.Children = await _progenyHttpClient.GetProgenyAdminList(userEmail);
            myFamily.FamilyMembers = new List<ApplicationUser>();
            myFamily.OtherMembers = new List<ApplicationUser>();
            myFamily.AccessList = new List<UserAccess>();
            if (myFamily.Children != null && myFamily.Children.Any())
            {
                foreach (Progeny prog in myFamily.Children)
                {
                    List<UserAccess> uaList = await _progenyHttpClient.GetProgenyAccessList(prog.Id);
                    myFamily.AccessList.AddRange(uaList);
                }
            }
             myFamily.AccessLevelList = new AccessLevelList();
            return View(myFamily);
        }
    }
}