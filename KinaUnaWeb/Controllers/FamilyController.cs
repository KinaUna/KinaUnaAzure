using System;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class FamilyController : Controller
    {
        
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;
        
        public FamilyController(IProgenyHttpClient progenyHttpClient, IUserAccessHttpClient userAccessHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyViewModel model = new FamilyViewModel(baseModel);
            
            model.Family = new Family();
            model.Family.Children = await _progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            
            if (model.Family.Children != null && model.Family.Children.Any())
            {
                foreach (Progeny progeny in model.Family.Children)
                {
                    if (progeny.BirthDay.HasValue)
                    {
                        progeny.BirthDay = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(progeny.BirthDay.Value,progeny.TimeZone,model.CurrentUser.Timezone);
                    }
                    List<UserAccess> userAccesses = await _userAccessHttpClient.GetProgenyAccessList(progeny.Id);
                    model.Family.AccessList.AddRange(userAccesses);
                }
            }
            
            model.Family.SetAccessLevelList(model.LanguageId);

            return View(model);
        }
    }
}