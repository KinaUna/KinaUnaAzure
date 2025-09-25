using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class FamiliesController(IFamiliesHttpClient familiesHttpClient, IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient, IUserGroupsHttpClient userGroupsHttpClient) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new FamiliesViewModel(baseModel);

            model.Families = await familiesHttpClient.GetMyFamilies();

            if (model.Families.Count == 0)
            {
                Family family = new Family();
                model.Families.Add(family);
            }
            else
            {
                foreach (Family family in model.Families)
                {
                    if (family.FamilyMembers.Count > 0)
                    {
                        foreach (FamilyMember familyMember in family.FamilyMembers)
                        {
                            if (familyMember.ProgenyId > 0)
                            {
                                familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                            }
                        }
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Members()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new FamiliesViewModel(baseModel);

            model.Families = await familiesHttpClient.GetMyFamilies();

            if (model.Families.Count == 0)
            {
                Family family = new Family();
                model.Families.Add(family);
            }
            else
            {
                foreach (Family family in model.Families)
                {
                    if (family.FamilyMembers.Count > 0)
                    {
                        foreach (FamilyMember familyMember in family.FamilyMembers)
                        {
                            if (familyMember.ProgenyId > 0)
                            {
                                familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                            }
                        }
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> UserAccess()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            UserGroupsViewModel model = new UserGroupsViewModel(baseModel);
            model.FamiliesList = await familiesHttpClient.GetMyFamilies();
            model.ProgenyList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);

            foreach (Family family in model.FamiliesList)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForFamily(family.FamilyId);
                model.UserGroups.AddRange(userGroupsList);
            }
            foreach (Progeny progeny in model.ProgenyList)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForProgeny(progeny.Id);
                model.UserGroups.AddRange(userGroupsList);
            }

            return View();
        }
    }
}
