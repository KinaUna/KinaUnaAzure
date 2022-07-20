using System;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.FamilyViewModels;

namespace KinaUnaWeb.Controllers
{
    public class FamilyController : Controller
    {
        
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public FamilyController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        public async Task<IActionResult> Index()
        {
            FamilyViewModel model = new FamilyViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            model.Family = new Family();
            model.Family.Children = await _progenyHttpClient.GetProgenyAdminList(userEmail);
            model.Family.FamilyMembers = new List<UserInfo>();
            model.Family.OtherMembers = new List<UserInfo>();
            model.Family.AccessList = new List<UserAccess>();
            if (model.Family.Children != null && model.Family.Children.Any())
            {
                foreach (Progeny prog in model.Family.Children)
                {
                    if (prog.BirthDay.HasValue)
                    {
                        prog.BirthDay = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(prog.BirthDay.Value,prog.TimeZone,model.CurrentUser.Timezone);
                    }
                    List<UserAccess> uaList = await _userAccessHttpClient.GetProgenyAccessList(prog.Id);
                    model.Family.AccessList.AddRange(uaList);
                }
            }

            model.Family.AccessLevelList = new AccessLevelList();

            if (model.LanguageId == 2)
            {
                model.Family.AccessLevelList.AccessLevelListEn = model.Family.AccessLevelList.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.Family.AccessLevelList.AccessLevelListEn = model.Family.AccessLevelList.AccessLevelListDa;
            }

            return View(model);
        }
    }
}