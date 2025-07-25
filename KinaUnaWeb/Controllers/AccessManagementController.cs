using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Access Management Controller. Handles UserAccess to Progeny.
    /// </summary>
    /// <param name="userInfosHttpClient"></param>
    /// <param name="userAccessHttpClient"></param>
    /// <param name="viewModelSetupService"></param>
    public class AccessManagementController(IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient, IViewModelSetupService viewModelSetupService)
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId);
            UserAccessViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser, progenyId);

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userAccess.ProgenyId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userAccess.ProgenyId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
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
    }
}