using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin = false;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public VaccinationsController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            if (_progId == 0)
            {
                _progId = 2;
            }
            Progeny progeny = new Progeny();
            progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = 0;
            }

            VaccinationViewModel model = new VaccinationViewModel();
            model.VaccinationList = new List<Vaccination>();
            List<Vaccination> vList = _context.VaccinationsDb.Where(v => v.ProgenyId == _progId).ToList();

            if (vList.Count != 0)
            {
                foreach (Vaccination v in vList)
                {
                    if (v.AccessLevel >= userAccessLevel)
                    {
                        model.VaccinationList.Add(v);
                    }

                }
                model.VaccinationList = model.VaccinationList.OrderBy(v => v.VaccinationDate).ToList();
            }
            else
            {
                Vaccination v = new Vaccination();
                v.ProgenyId = _progId;
                v.VaccinationName = "No vaccinations found.";
                v.VaccinationDescription = "The vaccinations list is empty.";

                model.VaccinationList.Add(v);
            }
            model.IsAdmin = _userIsProgenyAdmin;
            model.Progeny = progeny;
            return View(model);

        }
    }
}