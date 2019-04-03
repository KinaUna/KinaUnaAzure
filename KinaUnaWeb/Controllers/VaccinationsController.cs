using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public VaccinationsController(IProgenyHttpClient progenyHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

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
                userAccessLevel = (int)AccessLevel.Private;
            }

            VaccinationViewModel model = new VaccinationViewModel();
            model.VaccinationList = new List<Vaccination>();
            List<Vaccination> vaccinations = await _progenyHttpClient.GetVaccinationsList(_progId, userAccessLevel); // _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == _progId).ToList();

            if (vaccinations.Count != 0)
            {
                foreach (Vaccination v in vaccinations)
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
                Vaccination vaccination = new Vaccination();
                vaccination.ProgenyId = _progId;
                vaccination.VaccinationName = "No vaccinations found.";
                vaccination.VaccinationDescription = "The vaccinations list is empty.";

                model.VaccinationList.Add(vaccination);
            }
            model.IsAdmin = _userIsProgenyAdmin;
            model.Progeny = progeny;
            return View(model);

        }
    }
}