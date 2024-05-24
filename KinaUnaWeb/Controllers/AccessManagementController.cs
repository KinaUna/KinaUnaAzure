﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class AccessManagementController(IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient, IViewModelSetupService viewModelSetupService)
        : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        [HttpGet]
        public async Task<IActionResult> AddAccess(int progenyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId);
            UserAccessViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            
            if(!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            UserAccess userAccessToAdd = new()
            {
                ProgenyId = model.CurrentProgenyId,
                UserId = model.Email.ToUpper(),
                AccessLevel = model.AccessLevel
            };

            List<UserAccess> progenyAccessList = await userAccessHttpClient.GetUserAccessList(model.Email.ToUpper());
            
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == model.CurrentProgenyId);
            if (oldUserAccess == null)
            {
                _ = await userAccessHttpClient.AddUserAccess(userAccessToAdd);
            }
            else
            {
                _ = await userAccessHttpClient.DeleteUserAccess(oldUserAccess.AccessId);
                _ = await userAccessHttpClient.AddUserAccess(userAccessToAdd);
            }
            
            // Todo: Notify user of update
            
            return RedirectToAction("Index");
        }

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
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            UserAccess userAccess = model.CreateUserAccess();
            
            await userAccessHttpClient.UpdateUserAccess(userAccess);
            
            // Todo: Notify user of update
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAccess(int accessId)
        {
            UserAccess userAccess = await userAccessHttpClient.GetUserAccess(accessId);

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), userAccess.ProgenyId);
            UserAccessViewModel model = new(baseModel);

            UserInfo userAccessUserInfo = await userInfosHttpClient.GetUserInfo(userAccess.UserId);
            model.SetUserAccessItem(userAccess, userAccessUserInfo);
            model.SetAccessLevelList();

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccess(UserAccessViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            UserAccess accessToDelete = await userAccessHttpClient.GetUserAccess(model.AccessId);
            if (accessToDelete.ProgenyId == model.CurrentProgenyId)
            {
                await userAccessHttpClient.DeleteUserAccess(model.AccessId);
            }
            
            // Todo: Notify user of update
            return RedirectToAction("Index");
        }
    }
}