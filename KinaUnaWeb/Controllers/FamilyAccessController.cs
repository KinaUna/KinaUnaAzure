using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamilyAccessViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class FamilyAccessController(
        IFamiliesHttpClient familiesHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient,
        IUserGroupsHttpClient userGroupsHttpClient) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            PermissionsListViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };
            model.Progenies = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            
            return View(model);
        }

        public async Task<IActionResult> PermissionsList()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            PermissionsListViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };
            model.Progenies = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);

            foreach (Family family in model.Families)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForFamily(family.FamilyId);
                model.UserGroups.AddRange(userGroupsList);
                List<FamilyPermission> familyPermissions = await familiesHttpClient.GetFamilyPermissionsList(family.FamilyId);
                model.FamilyPermissions.AddRange(familyPermissions);
            }

            foreach (Progeny progeny in model.Progenies)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForProgeny(progeny.Id);
                model.UserGroups.AddRange(userGroupsList);
                List<ProgenyPermission> progenyPermissions = await progenyHttpClient.GetProgenyPermissionsList(progeny.Id);
                model.ProgenyPermissions.AddRange(progenyPermissions);
            }

            return PartialView("_PermissionsListPartial", model);
        }

        [HttpGet("[action]/{progenyId:int}/{familyId:int}")]
        public async Task<IActionResult> AddGroup(int progenyId, int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, familyId, false);
            UserGroupViewModel model = new(baseModel);

            if (progenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
                model.UserGroup = new UserGroup
                {
                    ProgenyId = progenyId
                };
            }
            if (familyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
                model.UserGroup = new UserGroup
                {
                    FamilyId = familyId
                };
            }

            return PartialView("_AddGroupPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGroup(UserGroupViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.UserGroup.ProgenyId, model.UserGroup.FamilyId, false);
            model.SetBaseProperties(baseModel);
            if (model.UserGroup.ProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            if (model.UserGroup.FamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }

            UserGroup newGroup = await userGroupsHttpClient.AddUserGroup(model.UserGroup);
            
            return Json(newGroup);
        }

        [HttpGet("[action]/{groupId:int}")]
        public async Task<IActionResult> EditGroup(int groupId)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(groupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
            UserGroupViewModel model = new(baseModel);
            model.UserGroup = userGroup;
            if (model.UserGroup == null)
            {
                return NotFound();
            }
            if (model.CurrentProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            if (model.CurrentFamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            return PartialView("_EditGroupPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(UserGroupViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.UserGroup.ProgenyId, model.UserGroup.FamilyId, false);
            model.SetBaseProperties(baseModel);
            if (model.CurrentProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            if (model.CurrentFamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }

            UserGroup updatedGroup = await userGroupsHttpClient.UpdateUserGroup(model.UserGroup);
            return Json(updatedGroup);
        }

        [HttpGet("[action]/{groupId:int}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(groupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
            UserGroupViewModel model = new(baseModel);
            model.UserGroup = userGroup;
            
            if (model.CurrentProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            if (model.CurrentFamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
            }
            return PartialView("_DeleteGroupPartial", model);
        }
    }
}
