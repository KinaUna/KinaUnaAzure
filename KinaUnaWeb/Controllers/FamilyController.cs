using System;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Provides pages and actions for managing family data.
    /// </summary>
    /// <param name="progenyHttpClient"></param>
    /// <param name="userAccessHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class FamilyController(IProgenyHttpClient progenyHttpClient, IUserAccessHttpClient userAccessHttpClient, IUserInfosHttpClient userInfosHttpClient, IViewModelSetupService viewModelSetupService)
        : Controller
    {
        /// <summary>
        /// The Family Index page.
        /// Shows a list of all Progeny and the user access lists for each.
        /// </summary>
        /// <returns>View with FamilyViewModel.</returns>
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyViewModel model = new(baseModel)
            {
                FamilyDto = new FamilyDTO()
            };
            model.FamilyDto.Children = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);

            if (model.FamilyDto.Children != null && model.FamilyDto.Children.Count != 0)
            {
                foreach (Progeny progeny in model.FamilyDto.Children)
                {
                    if (progeny.BirthDay.HasValue)
                    {
                        progeny.BirthDay = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(progeny.BirthDay.Value,progeny.TimeZone,model.CurrentUser.Timezone);
                    }
                    List<UserAccess> userAccesses = await userAccessHttpClient.GetProgenyAccessList(progeny.Id);
                    foreach (UserAccess userAccess in userAccesses)
                    {
                        userAccess.Progeny = await progenyHttpClient.GetProgeny(userAccess.ProgenyId);
                        userAccess.User = await userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    }

                    model.FamilyDto.AccessList.AddRange(userAccesses);
                }
            }
            
            model.FamilyDto.SetAccessLevelList(model.LanguageId);

            return View(model);
        }
    }
}