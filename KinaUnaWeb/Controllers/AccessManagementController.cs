using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class AccessManagementController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        private int _progenyId = Constants.DefaultChildId;
        public AccessManagementController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        [HttpGet]
        public async Task<IActionResult> AddAccess(string progenyId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();

            string userEmail = User.GetEmail() ?? _defaultUser;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progenyId = userinfo.ViewChild;
            }
            
            model.ProgenyId = int.Parse(progenyId);
            if (progenyId == "0")
            {
                progenyId = userinfo.ViewChild.ToString();
            }
            model.Progeny = await _progenyHttpClient.GetProgeny(int.Parse(progenyId));
            model.ProgenyName = model.Progeny.Name;
            model.Email = "";
            model.AccessLevel = (int)AccessLevel.Users;
            model.UserId = "";
            model.ProgenyList = new List<SelectListItem>();
            if (User.Identity != null && (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null))
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == _progenyId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            // Todo: Access level list translations.

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(UserAccessViewModel model)
        {
            string userEmail = User.GetEmail() ?? _defaultUser;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (userinfo != null && userinfo.ViewChild > 0)
            {
                _progenyId = userinfo.ViewChild;
            }

            UserAccess accessModel = new UserAccess();
            accessModel.ProgenyId = model.ProgenyId;
            accessModel.UserId = model.Email.ToUpper();
            accessModel.AccessLevel = model.AccessLevel;
            List<UserAccess> progenyAccessList = await _userAccessHttpClient.GetUserAccessList(model.Email.ToUpper());
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == model.ProgenyId);
            if (oldUserAccess == null)
            {
                await _userAccessHttpClient.AddUserAccess(accessModel);
            }
            else
            {
                await _userAccessHttpClient.DeleteUserAccess(oldUserAccess.AccessId);
                await _userAccessHttpClient.AddUserAccess(accessModel);
            }
            
            // Todo: Notify user of update
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditAccess(string accessId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            UserAccess userAccess = await _userAccessHttpClient.GetUserAccess(Int32.Parse(accessId));
            model.ProgenyId = userAccess.ProgenyId;
            model.UserId = userAccess.UserId;
            model.AccessId = userAccess.AccessId;
            model.AccessLevel = userAccess.AccessLevel;
            model.Email = userAccess.UserId;
            model.UserName = "No user found";
            model.FirstName = "No user found";
            model.MiddleName = "No user found";
            model.LastName = "No user found";
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
            if (userInfo != null)
            {
                model.Email = userInfo.UserEmail;
                model.UserName = userInfo.UserName;
                model.FirstName = userInfo.FirstName;
                model.MiddleName = userInfo.MiddleName;
                model.LastName = userInfo.LastName;
            }

            model.Progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            model.ProgenyName = model.Progeny.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccess(UserAccessViewModel model)
        {
            UserAccess userAccess = new UserAccess();
            userAccess.AccessId = model.AccessId;
            userAccess.ProgenyId = model.ProgenyId;
            userAccess.UserId = model.Email;
            userAccess.AccessLevel = model.AccessLevel;
            await _userAccessHttpClient.UpdateUserAccess(userAccess);
            
            // To do: Notify user of update
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAccess(int accessId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            UserAccess userAccess = await _userAccessHttpClient.GetUserAccess(accessId);
            model.ProgenyId = userAccess.ProgenyId;
            model.UserId = userAccess.UserId;
            model.AccessId = userAccess.AccessId;
            model.AccessLevel = userAccess.AccessLevel;
            model.Email = userAccess.UserId;
            model.UserName = "No user found";
            model.FirstName = "No user found";
            model.MiddleName = "No user found";
            model.LastName = "No user found";
            UserInfo appUser = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
            if (appUser != null)
            {
                model.Email = appUser.UserEmail;
                model.UserName = appUser.UserName;
                model.FirstName = appUser.FirstName;
                model.MiddleName = appUser.MiddleName;
                model.LastName = appUser.LastName;
            }
            model.Progeny = await _progenyHttpClient.GetProgeny(userAccess.ProgenyId);
            model.ProgenyName = model.Progeny.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccess(UserAccessViewModel model)
        {
            await _userAccessHttpClient.GetUserAccess(model.AccessId);
            await _userAccessHttpClient.DeleteUserAccess(model.AccessId);
            
            // To do: Notify user of update
            return RedirectToAction("Index");
        }
    }
}