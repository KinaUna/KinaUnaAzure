using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IVaccinationsHttpClient _vaccinationsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public VaccinationsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IVaccinationsHttpClient vaccinationsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _vaccinationsHttpClient = vaccinationsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);


            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            model.VaccinationList = new List<Vaccination>();
            List<Vaccination> vaccinations = await _vaccinationsHttpClient.GetVaccinationsList(childId, userAccessLevel);

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

            model.Progeny = progeny;
            
            return View(model);
        }
    }
}