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
        IProgenyHttpClient progenyHttpClient, IUserGroupsHttpClient userGroupsHttpClient, IUserInfosHttpClient userInfosHttpClient) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new(baseModel);

            return View(model);
        }

        public async Task<IActionResult> FamiliesList()
        {
            List<Family> families = await familiesHttpClient.GetMyFamilies();

            return Json(families);
        }

        public async Task<IActionResult> FamilyElement(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyElementViewModel model = new(baseModel)
            {
                Family = await familiesHttpClient.GetFamily(familyId)
            };

            if (model.Family.FamilyMembers.Count > 0)
            {
                foreach (FamilyMember familyMember in model.Family.FamilyMembers)
                {
                    if (familyMember.ProgenyId > 0)
                    {
                        familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(familyMember.UserId))
                        {
                            familyMember.UserInfo = await userInfosHttpClient.GetUserInfoByUserId(familyMember.UserId);
                        }
                    }
                }
            }
            
            return PartialView("_FamilyElementPartial", model);
        }

        public async Task<IActionResult> FamilyDetails(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyDetailsViewModel model = new(baseModel)
            {
                Family = await familiesHttpClient.GetFamily(familyId)
            };

            if (model.Family.FamilyMembers.Count > 0)
            {
                foreach (FamilyMember familyMember in model.Family.FamilyMembers)
                {
                    if (familyMember.ProgenyId > 0)
                    {
                        familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(familyMember.UserId))
                        {
                            familyMember.UserInfo = await userInfosHttpClient.GetUserInfoByUserId(familyMember.UserId);
                        }
                    }
                }
            }

            return PartialView("_FamilyDetailsPartial", model);
        }

        public async Task<IActionResult> AddFamily()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyDetailsViewModel model = new(baseModel);

            Family family = new();
            model.Family = family;

            return PartialView("_AddFamilyPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFamily([FromForm] Family family)
        {
            Family addedFamily = await familiesHttpClient.AddFamily(family);

            return Json(addedFamily);
        }

        public async Task<IActionResult> EditFamily(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamilyDetailsViewModel model = new(baseModel);
            model.Family = await familiesHttpClient.GetFamily(familyId);

            return PartialView("_EditFamilyPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFamily([FromForm] Family family)
        {
            Family updatedFamily = await familiesHttpClient.UpdateFamily(family);

            return Json(updatedFamily);
        }

        public async Task<IActionResult> Members()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };

            if (model.Families.Count == 0)
            {
                Family family = new();
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
            UserGroupsViewModel model = new(baseModel)
            {
                FamiliesList = await familiesHttpClient.GetMyFamilies()
            };
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

            return View(model);
        }
    }
}
