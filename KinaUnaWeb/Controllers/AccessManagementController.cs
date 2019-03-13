using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
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
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        private int _progId = Constants.DefaultChildId;

        public AccessManagementController(IProgenyHttpClient progenyHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        [HttpGet]
        public async Task<IActionResult> AddAccess(string progenyId)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            UserAccessViewModel model = new UserAccessViewModel();
            model.ProgenyId = Int32.Parse(progenyId);
            if (progenyId == "0")
            {
                progenyId = userinfo.ViewChild.ToString();
            }
            model.Progeny = await _progenyHttpClient.GetProgeny(Int32.Parse(progenyId));
            model.ProgenyName = model.Progeny.Name;
            model.Email = "";
            model.AccessLevel = (int)AccessLevel.Users;
            model.UserId = "";
            model.ProgenyList = new List<SelectListItem>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                var accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(UserAccessViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo != null && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            UserAccess accessModel = new UserAccess();
            accessModel.ProgenyId = model.ProgenyId;
            accessModel.UserId = model.Email.ToUpper();
            accessModel.AccessLevel = model.AccessLevel;
            var progenyAccessList = await _progenyHttpClient.GetUserAccessList(model.Email.ToUpper());
            var oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == model.ProgenyId);
            if (oldUserAccess == null)
            {
                await _progenyHttpClient.AddUserAccess(accessModel);
            }
            else
            {
                await _progenyHttpClient.DeleteUserAccess(oldUserAccess.AccessId);
                await _progenyHttpClient.AddUserAccess(accessModel);
            }
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (model.AccessLevel == 0 && !progeny.Admins.ToUpper().Contains(model.UserId.ToUpper()))
            {
                
                progeny.Admins = progeny.Admins + ", " + (accessModel.UserId).ToUpper();
                await _progenyHttpClient.UpdateProgeny(progeny);
            }

            // Todo: Notify user of update
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditAccess(string accessId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            UserAccess uaModel = await _progenyHttpClient.GetUserAccess(Int32.Parse(accessId));
            model.ProgenyId = uaModel.ProgenyId;
            model.UserId = uaModel.UserId;
            model.AccessId = uaModel.AccessId;
            model.AccessLevel = uaModel.AccessLevel;
            model.Email = uaModel.UserId;
            model.UserName = "No user found";
            model.FirstName = "No user found";
            model.MiddleName = "No user found";
            model.LastName = "No user found";
            UserInfo appUser = await _progenyHttpClient.GetUserInfo(uaModel.UserId);
            if (appUser != null)
            {
                model.Email = appUser.UserEmail;
                model.UserName = appUser.UserName;
                model.FirstName = appUser.FirstName;
                model.MiddleName = appUser.MiddleName;
                model.LastName = appUser.LastName;
            }

            model.Progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            model.ProgenyName = model.Progeny.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccess(UserAccessViewModel model)
        {
            UserAccess uaModel = new UserAccess();
            uaModel.AccessId = model.AccessId;
            uaModel.ProgenyId = model.ProgenyId;
            uaModel.UserId = model.Email;
            uaModel.AccessLevel = model.AccessLevel;
            await _progenyHttpClient.UpdateUserAccess(uaModel);

            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (model.AccessLevel == (int)AccessLevel.Private && !progeny.Admins.ToUpper().Contains(model.UserId.ToUpper()))
            {

                progeny.Admins = progeny.Admins + ", " + (model.Email).ToUpper();
                await _progenyHttpClient.UpdateProgeny(progeny);
            }
            
            // To do: Notify user of update
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAccess(int accessId)
        {
            UserAccessViewModel model = new UserAccessViewModel();
            UserAccess uaModel = await _progenyHttpClient.GetUserAccess(accessId);
            model.ProgenyId = uaModel.ProgenyId;
            model.UserId = uaModel.UserId;
            model.AccessId = uaModel.AccessId;
            model.AccessLevel = uaModel.AccessLevel;
            model.Email = uaModel.UserId;
            model.UserName = "No user found";
            model.FirstName = "No user found";
            model.MiddleName = "No user found";
            model.LastName = "No user found";
            UserInfo appUser = await _progenyHttpClient.GetUserInfo(uaModel.UserId);
            if (appUser != null)
            {
                model.Email = appUser.UserEmail;
                model.UserName = appUser.UserName;
                model.FirstName = appUser.FirstName;
                model.MiddleName = appUser.MiddleName;
                model.LastName = appUser.LastName;
            }
            model.Progeny = await _progenyHttpClient.GetProgeny(uaModel.ProgenyId);
            model.ProgenyName = model.Progeny.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccess(UserAccessViewModel model)
        {
            await _progenyHttpClient.GetUserAccess(model.AccessId);
            await _progenyHttpClient.DeleteUserAccess(model.AccessId);

            if (model.AccessLevel == 0)
            {
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                progeny.Admins = progeny.Admins.Replace(model.Email.ToUpper(), "");
                progeny.Admins = progeny.Admins.Replace(",,", "");
                if (progeny.Admins.StartsWith(","))
                {
                    progeny.Admins.Remove(0);
                }
                await _progenyHttpClient.UpdateProgeny(progeny);
            }

            // To do: Notify user of update
            return RedirectToAction("Index");
        }
    }
}