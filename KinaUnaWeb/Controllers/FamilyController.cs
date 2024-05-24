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
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class FamilyController(IProgenyHttpClient progenyHttpClient, IUserAccessHttpClient userAccessHttpClient, IViewModelSetupService viewModelSetupService)
        : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyViewModel model = new(baseModel)
            {
                Family = new()
            };
            model.Family.Children = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);

            if (model.Family.Children != null && model.Family.Children.Any())
            {
                foreach (Progeny progeny in model.Family.Children)
                {
                    if (progeny.BirthDay.HasValue)
                    {
                        progeny.BirthDay = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(progeny.BirthDay.Value,progeny.TimeZone,model.CurrentUser.Timezone);
                    }
                    List<UserAccess> userAccesses = await userAccessHttpClient.GetProgenyAccessList(progeny.Id);
                    model.Family.AccessList.AddRange(userAccesses);
                }
            }
            
            model.Family.SetAccessLevelList(model.LanguageId);

            return View(model);
        }
    }
}