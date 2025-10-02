using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Models.ProgeniesViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.FileManager.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Access Management Controller. Handles UserAccess to Progeny.
    /// </summary>
    /// <param name="userInfosHttpClient"></param>
    /// <param name="userAccessHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class AccessManagementController(IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient,
        IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient, IFamiliesHttpClient familiesHttpClient,
        IUserGroupsHttpClient userGroupsHttpClient)
        : Controller
    {
        /// <summary>
        /// Index page for the AccessManagementController. Redirects to the FamilyController Index page.
        /// </summary>
        /// <returns>Redirect to the Family Index page.</returns>
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        /// <summary>
        /// Page for adding a new user access to a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to add a UserAccess for.</param>
        /// <returns>View with UserAccessViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddAccess(int progenyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, 0);
            UserAccessViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(progenyId);

            model.SetAccessLevelList();

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            return PartialView("_AddAccessPartial", model);
        }

        /// <summary>
        /// Post action for adding a new user access to a Progeny.
        /// </summary>
        /// <param name="model">UserAccessViewModel with the Progeny's Id, user's email, and the access level to assign to the user for the progeny.</param>
        /// <returns>Redirection to Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId, 0);
            model.SetBaseProperties(baseModel);
            
            if(!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            UserAccess userAccessToAdd = new()
            {
                ProgenyId = model.CurrentProgenyId,
                UserId = model.Email.ToUpper(),
                AccessLevel = model.AccessLevel
            };

            List<UserAccess> progenyAccessList = await userAccessHttpClient.GetUserAccessList(model.Email.ToUpper());
            
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == model.CurrentProgenyId);
            if (oldUserAccess != null)
            {
                _ = await userAccessHttpClient.DeleteUserAccess(oldUserAccess.AccessId);
            }

            _ = await userAccessHttpClient.AddUserAccess(userAccessToAdd);

            return PartialView("_NewAccessPartial", model);
        }

        /// <summary>
        /// Page for editing an existing UserAccess.
        /// </summary>
        /// <param name="accessId">The AccessId of the UserAccess item to edit.</param>
        /// <returns>View with a UserAccessModel. If the current user isn't admin for the Progeny redirects to Home/Index.</returns>
        [HttpGet]
        public async Task<IActionResult> EditAccess(int accessId)
        {
            UserAccess userAccess = await userAccessHttpClient.GetUserAccess(accessId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userAccess.ProgenyId, 0);
            UserAccessViewModel model = new(baseModel);

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userAccess.UserId);
            
            model.SetUserAccessItem(userAccess, userInfo);
            
            model.ProgenyName = model.CurrentProgeny.Name;

            model.SetAccessLevelList();

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            return PartialView("_EditAccessPartial", model);
        }

        /// <summary>
        /// Post action for editing an existing UserAccess.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Redirection to Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId, 0);
            model.SetBaseProperties(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            UserAccess userAccess = model.CreateUserAccess();

            userAccess = await userAccessHttpClient.UpdateUserAccess(userAccess);

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userAccess.UserId);

            model.SetUserAccessItem(userAccess, userInfo);

            model.ProgenyName = model.CurrentProgeny.Name;

            model.SetAccessLevelList();

            return PartialView("_UpdatedAccessPartial", model);
        }

        /// <summary>
        /// Page for deleting an existing UserAccess.
        /// </summary>
        /// <param name="accessId">The AccessId of the UserAccess to delete.</param>
        /// <returns>View with UserAccessViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteAccess(int accessId)
        {
            UserAccess userAccess = await userAccessHttpClient.GetUserAccess(accessId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userAccess.ProgenyId, 0);
            UserAccessViewModel model = new(baseModel);

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userAccess.UserId);

            model.SetUserAccessItem(userAccess, userInfo);

            model.ProgenyName = model.CurrentProgeny.Name;

            model.SetAccessLevelList();

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                if (!model.CurrentUser.UserEmail.Equals(userAccess.UserId, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    return PartialView("_AccessDeniedPartial");
                }

                return PartialView("_DeleteMyAccessPartial", model);
            }

            return PartialView("_DeleteAccessPartial", model);
        }

        /// <summary>
        /// Post action for deleting an existing UserAccess.
        /// </summary>
        /// <param name="model">UserAccessViewModel.</param>
        /// <returns>Redirect to Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId, 0);
            model.SetBaseProperties(baseModel);
            
            UserAccess accessToDelete = await userAccessHttpClient.GetUserAccess(model.AccessId);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail) && !model.CurrentUser.UserEmail.Equals(accessToDelete.UserId, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return PartialView("_AccessDeniedPartial");
            }

            
            if (accessToDelete.ProgenyId == model.CurrentProgenyId)
            { 
                await userAccessHttpClient.DeleteUserAccess(model.AccessId);
            }

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(accessToDelete.UserId);

            model.SetUserAccessItem(accessToDelete, userInfo);

            model.ProgenyName = model.CurrentProgeny.Name;

            model.SetAccessLevelList();

            if (!model.CurrentUser.UserEmail.Equals(accessToDelete.UserId, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return PartialView("_DeletedMyAccessPartial", model);
            }

            return PartialView("_DeletedAccessPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> ProgenyItemPermissionsModal(int progenyId, int itemType, int itemId)
        {
            ProgenyItemPermissionsViewModel model = new()
            {
                ItemId = itemId,
                ItemType = (KinaUnaTypes.TimeLineType)itemType,
                UserGroupsList = await userGroupsHttpClient.GetUserGroupsForProgeny(progenyId),
                ProgenyPermissionsList = await progenyHttpClient.GetProgenyPermissionsList(progenyId)
            };

            foreach (ProgenyPermission permission in model.ProgenyPermissionsList)
            {
                if (string.IsNullOrWhiteSpace(permission.UserId)) continue;
                UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(permission.UserId);
                model.UserList.Add(userInfo);
            }

            return PartialView("_ProgenyItemPermissionsPartial", model);
        }

        [HttpGet]
        public async Task<IActionResult> FamilyItemPermissionsModal(int familyId, int itemType, int itemId)
        {
            FamilyItemPermissionsViewModel model = new()
            {
                ItemId = itemId,
                ItemType = (KinaUnaTypes.TimeLineType)itemType,
                UserGroupsList = await userGroupsHttpClient.GetUserGroupsForFamily(familyId),
                FamilyPermissionsList = await familiesHttpClient.GetFamilyPermissionsList(familyId)
            };

            foreach(FamilyPermission permission in model.FamilyPermissionsList)
            {
                if (string.IsNullOrWhiteSpace(permission.UserId)) continue;
                UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(permission.UserId);
                model.UserList.Add(userInfo);
            }

            return PartialView("_FamilyItemPermissionsPartial", model);
        }

    }
}