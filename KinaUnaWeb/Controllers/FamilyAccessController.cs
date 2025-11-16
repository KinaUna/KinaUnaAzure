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
            model.Progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Edit);

            return View(model);
        }

        public async Task<IActionResult> PermissionsList()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            PermissionsListViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };
            model.Progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Admin);

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

        [HttpGet]
        [Route("[controller]/[action]/{progenyId:int}/{familyId:int}")]
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

            model.SetPermissionsLevelsList();

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

            model.UserGroup.PermissionLevel = model.PermissionLevel;
            UserGroup newGroup = await userGroupsHttpClient.AddUserGroup(model.UserGroup);

            return Json(newGroup);
        }

        [HttpGet]
        [Route("[controller]/[action]/{groupId:int}")]
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
            model.PermissionLevel = model.UserGroup.PermissionLevel;
            model.SetPermissionsLevelsList();

            return PartialView("_EditGroupPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(UserGroupViewModel model)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(model.UserGroup.UserGroupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }

            if (userGroup.PermissionLevel < PermissionLevel.Admin)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
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

            model.UserGroup.PermissionLevel = model.PermissionLevel;
            UserGroup updatedGroup = await userGroupsHttpClient.UpdateUserGroup(model.UserGroup);
            return Json(updatedGroup);
        }

        [HttpGet]
        [Route("[controller]/[action]/{groupId:int}")]
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

            model.PermissionLevel = model.UserGroup.PermissionLevel;
            return PartialView("_DeleteGroupPartial", model);
        }

        [HttpGet]
        [Route("[controller]/[action]/{groupId:int}")]
        public async Task<IActionResult> AddGroupMember(int groupId)
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

            model.UserGroupMember = new UserGroupMember
            {
                UserGroupId = groupId
            };

            return PartialView("_AddGroupMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGroupMember(UserGroupViewModel model)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(model.UserGroup.UserGroupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
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

            model.UserGroupMember.UserGroupId = userGroup.UserGroupId;

            UserGroupMember newMember = await userGroupsHttpClient.AddUserGroupMember(model.UserGroupMember);
            return Json(newMember);
        }

        [HttpGet]
        [Route("[controller]/[action]/{groupMemberId:int}")]
        public async Task<IActionResult> EditGroupMember(int groupMemberId)
        {
            UserGroupMember userGroupMember = await userGroupsHttpClient.GetUserGroupMember(groupMemberId);
            if (userGroupMember == null || userGroupMember.UserGroupMemberId == 0)
            {
                return NotFound();
            }

            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(userGroupMember.UserGroupId);
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

            model.UserGroupMember = userGroupMember;

            return PartialView("_EditGroupMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroupMember(UserGroupViewModel model)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(model.UserGroupMember.UserGroupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
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

            UserGroupMember groupMember = await userGroupsHttpClient.GetUserGroupMember(model.UserGroupMember.UserGroupMemberId);
            if (groupMember == null || groupMember.UserGroupMemberId == 0)
            {
                return NotFound();
            }
            groupMember.Email = model.UserGroupMember.Email;

            UserGroupMember updatedMember = await userGroupsHttpClient.UpdateUserGroupMember(groupMember);

            return Json(updatedMember);
        }

        [HttpGet]
        [Route("[controller]/[action]/{groupMemberId:int}")]
        public async Task<IActionResult> DeleteGroupMember(int groupMemberId)
        {
            UserGroupMember userGroupMember = await userGroupsHttpClient.GetUserGroupMember(groupMemberId);
            if (userGroupMember == null || userGroupMember.UserGroupMemberId == 0)
            {
                return NotFound();
            }
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(userGroupMember.UserGroupId);
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
            model.UserGroupMember = userGroupMember;
            return PartialView("_DeleteGroupMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroupMember(UserGroupViewModel model)
        {
            UserGroup userGroup = await userGroupsHttpClient.GetUserGroup(model.UserGroupMember.UserGroupId);
            if (userGroup == null || userGroup.UserGroupId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userGroup.ProgenyId, userGroup.FamilyId, false);
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
            bool result = await userGroupsHttpClient.RemoveUserGroupMember(model.UserGroupMember.UserGroupMemberId);
            return Json(result);
        }

        [HttpGet]
        [Route("[controller]/[action]/{progenyId:int}/{familyId:int}")]
        public async Task<IActionResult> AddPermission(int progenyId, int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, familyId, false);
            PermissionViewModel model = new(baseModel);
            if (progenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
                model.ProgenyPermission = new ProgenyPermission
                {
                    ProgenyId = progenyId,
                    PermissionLevel = PermissionLevel.View
                };
            }
            if (familyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
                model.FamilyPermission = new FamilyPermission
                {
                    FamilyId = familyId,
                    PermissionLevel = PermissionLevel.View
                };
            }

            return PartialView("_AddPermissionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPermission(PermissionViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ProgenyPermission?.ProgenyId ?? 0, model.FamilyPermission?.FamilyId ?? 0, false);
            model.SetBaseProperties(baseModel);
            if (model.ProgenyPermission != null && model.ProgenyPermission.ProgenyId > 0)
            {
                if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }
                model.ProgenyPermission.PermissionLevel = model.PermissionLevel;
                model.ProgenyPermission.ProgenyId = model.CurrentProgenyId;
                ProgenyPermission newProgenyPermission = await progenyHttpClient.AddProgenyPermission(model.ProgenyPermission);
                return Json(newProgenyPermission);
            }
            if (model.FamilyPermission != null && model.FamilyPermission.FamilyId > 0)
            {
                if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
                {
                    return Forbid();
                }

                model.FamilyPermission.PermissionLevel = model.PermissionLevel;
                model.FamilyPermission.FamilyId = model.CurrentFamilyId;
                FamilyPermission newFamilyPermission = await familiesHttpClient.AddFamilyPermission(model.FamilyPermission);
                return Json(newFamilyPermission);
            }
            return Json(null);
        }

        [HttpGet]
        [Route("[controller]/[action]/{permissionId:int}")]
        public async Task<IActionResult> EditFamilyPermission(int permissionId)
        {
            FamilyPermission familyPermission = await familiesHttpClient.GetFamilyPermission(permissionId);
            if (familyPermission == null || familyPermission.FamilyPermissionId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyPermission.FamilyId, false);
            PermissionViewModel model = new(baseModel);
            model.FamilyPermission = familyPermission;
            if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.SetPermissionsLevelsList();

            return PartialView("_EditFamilyPermissionPartial", model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFamilyPermission(PermissionViewModel model)
        {
            FamilyPermission familyPermission = await familiesHttpClient.GetFamilyPermission(model.FamilyPermission.FamilyPermissionId);
            if (familyPermission == null || familyPermission.FamilyPermissionId == 0)
            {
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, model.FamilyPermission.FamilyId, false);
            model.SetBaseProperties(baseModel);
            if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.FamilyPermission.PermissionLevel = model.PermissionLevel;
            FamilyPermission updatedFamilyPermission = await familiesHttpClient.UpdateFamilyPermission(model.FamilyPermission);
            return Json(updatedFamilyPermission);
        }


        [HttpGet]
        [Route("[controller]/[action]/{permissionId:int}")]
        public async Task<IActionResult> EditProgenyPermission(int permissionId)
        {
            ProgenyPermission progenyPermission = await progenyHttpClient.GetProgenyPermission(permissionId);
            if (progenyPermission == null || progenyPermission.ProgenyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyPermission.ProgenyId, 0, false);
            PermissionViewModel model = new(baseModel);
            model.ProgenyPermission = progenyPermission;
            if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.SetPermissionsLevelsList();

            return PartialView("_EditProgenyPermissionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgenyPermission(PermissionViewModel model)
        {
            ProgenyPermission progenyPermission = await progenyHttpClient.GetProgenyPermission(model.ProgenyPermission.ProgenyPermissionId);
            if (progenyPermission == null || progenyPermission.ProgenyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ProgenyPermission.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.ProgenyPermission.PermissionLevel = model.PermissionLevel;

            ProgenyPermission updatedProgenyPermission = await progenyHttpClient.UpdateProgenyPermission(model.ProgenyPermission);
            return Json(updatedProgenyPermission);
        }

        [HttpGet]
        [Route("[controller]/[action]/{permissionId:int}")]
        public async Task<IActionResult> DeleteFamilyPermission(int permissionId)
        {
            FamilyPermission familyPermission = await familiesHttpClient.GetFamilyPermission(permissionId);
            if (familyPermission == null || familyPermission.FamilyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyPermission.FamilyId, false);
            PermissionViewModel model = new(baseModel);
            model.FamilyPermission = familyPermission;
            if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.PermissionLevel = model.FamilyPermission.PermissionLevel;
            model.SetPermissionsLevelsList();

            return PartialView("_DeleteFamilyPermissionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFamilyPermission(PermissionViewModel model)
        {
            FamilyPermission familyPermission = await familiesHttpClient.GetFamilyPermission(model.FamilyPermission.FamilyPermissionId);
            if (familyPermission == null || familyPermission.FamilyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, model.FamilyPermission.FamilyId, false);
            model.SetBaseProperties(baseModel);
            if (model.CurrentFamily.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            bool result = await familiesHttpClient.DeleteFamilyPermission(model.FamilyPermission.FamilyPermissionId);
            return Json(result);
        }

        [HttpGet]
        [Route("[controller]/[action]/{permissionId:int}")]
        public async Task<IActionResult> DeleteProgenyPermission(int permissionId)
        {
            ProgenyPermission progenyPermission = await progenyHttpClient.GetProgenyPermission(permissionId);
            if (progenyPermission == null || progenyPermission.ProgenyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyPermission.ProgenyId, 0, false);
            PermissionViewModel model = new(baseModel);
            model.ProgenyPermission = progenyPermission;
            if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            model.PermissionLevel = model.ProgenyPermission.PermissionLevel;
            model.SetPermissionsLevelsList();

            return PartialView("_DeleteProgenyPermissionPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgenyPermission(PermissionViewModel model)
        {
            ProgenyPermission progenyPermission = await progenyHttpClient.GetProgenyPermission(model.ProgenyPermission.ProgenyPermissionId);
            if (progenyPermission == null || progenyPermission.ProgenyPermissionId == 0)
            {
                return NotFound();
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.ProgenyPermission.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            if (model.CurrentProgeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return Forbid();
            }
            bool result = await progenyHttpClient.DeleteProgenyPermission(model.ProgenyPermission.ProgenyPermissionId);
            return Json(result);
        }
    }
}
