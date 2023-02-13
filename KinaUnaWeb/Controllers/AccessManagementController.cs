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
        public async Task<IActionResult> AddAccess(int progenyId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();

            string userEmail = User.GetEmail() ?? _defaultUser;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyId = progenyId;

            if (progenyId == 0 && userinfo.ViewChild > 0)
            {
                model.ProgenyId = userinfo.ViewChild;
            }
            
            model.Progeny = await _progenyHttpClient.GetProgeny(progenyId);
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
                        if (prog.Id == model.ProgenyId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            
            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(UserAccessViewModel model)
        {
            string userEmail = User.GetEmail() ?? _defaultUser;
            
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if(!progeny.IsInAdminList(userEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            UserAccess userAccessToAdd = new UserAccess();
            userAccessToAdd.ProgenyId = model.ProgenyId;
            userAccessToAdd.UserId = model.Email.ToUpper();
            userAccessToAdd.AccessLevel = model.AccessLevel;
            
            List<UserAccess> progenyAccessList = await _userAccessHttpClient.GetUserAccessList(model.Email.ToUpper());
            
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == model.ProgenyId);
            if (oldUserAccess == null)
            {
                await _userAccessHttpClient.AddUserAccess(userAccessToAdd);
            }
            else
            {
                await _userAccessHttpClient.DeleteUserAccess(oldUserAccess.AccessId);
                await _userAccessHttpClient.AddUserAccess(userAccessToAdd);
            }
            
            // Todo: Notify user of update
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditAccess(int accessId)
        {
            string userEmail = User.GetEmail() ?? _defaultUser;

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

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            if (!model.Progeny.IsInAdminList(userEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccess(UserAccessViewModel model)
        {
            string userEmail = User.GetEmail() ?? _defaultUser;
            model.Progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!model.Progeny.IsInAdminList(userEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            UserAccess userAccess = new UserAccess();
            userAccess.AccessId = model.AccessId;
            userAccess.ProgenyId = model.ProgenyId;
            userAccess.UserId = model.Email;
            userAccess.AccessLevel = model.AccessLevel;
            await _userAccessHttpClient.UpdateUserAccess(userAccess);
            
            // Todo: Notify user of update
            
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
            UserInfo userAccessUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
            if (userAccessUserInfo != null)
            {
                model.Email = userAccessUserInfo.UserEmail;
                model.UserName = userAccessUserInfo.UserName;
                model.FirstName = userAccessUserInfo.FirstName;
                model.MiddleName = userAccessUserInfo.MiddleName;
                model.LastName = userAccessUserInfo.LastName;
            }
            model.Progeny = await _progenyHttpClient.GetProgeny(userAccess.ProgenyId);
            model.ProgenyName = model.Progeny.Name;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            string userEmail = User.GetEmail() ?? _defaultUser;
            if (!model.Progeny.IsInAdminList(userEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccess(UserAccessViewModel model)
        {
            string userEmail = User.GetEmail() ?? _defaultUser;
            model.Progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!model.Progeny.IsInAdminList(userEmail))
            {
                return RedirectToAction("Index", "Home");
            }

            await _userAccessHttpClient.GetUserAccess(model.AccessId);
            await _userAccessHttpClient.DeleteUserAccess(model.AccessId);
            
            // Todo: Notify user of update
            return RedirectToAction("Index");
        }
    }
}