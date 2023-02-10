using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IVaccinationsHttpClient _vaccinationsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;

        public VaccinationsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IVaccinationsHttpClient vaccinationsHttpClient,
            IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _vaccinationsHttpClient = vaccinationsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);


            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            model.VaccinationList = new List<Vaccination>();
            List<Vaccination> vaccinations = await _vaccinationsHttpClient.GetVaccinationsList(childId, userAccessLevel);

            if (vaccinations.Count != 0)
            {
                foreach (Vaccination v in vaccinations)
                {
                    if (v.AccessLevel >= userAccessLevel)
                    {
                        model.VaccinationList.Add(v);
                    }

                }
                model.VaccinationList = model.VaccinationList.OrderBy(v => v.VaccinationDate).ToList();
            }

            model.Progeny = progeny;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];

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
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Vaccination vacItem = new Vaccination();
            vacItem.VaccinationName = model.VaccinationName;
            vacItem.ProgenyId = model.ProgenyId;
            vacItem.VaccinationDescription = model.VaccinationDescription;
            vacItem.VaccinationDate = model.VaccinationDate;
            vacItem.Notes = model.Notes;
            vacItem.AccessLevel = model.AccessLevel;
            vacItem.Author = model.CurrentUser.UserId;

            await _vaccinationsHttpClient.AddVaccination(vacItem);

            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vacItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + vacItem.VaccinationName + "\r\nContext: " + vacItem.VaccinationDate.ToString("dd-MMM-yyyy");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new vaccination was added for " + progeny.NickName;
                        notification.Link = "/Vaccinations?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.VaccinationId = vaccination.VaccinationId;
            model.ProgenyId = vaccination.ProgenyId;
            model.AccessLevel = vaccination.AccessLevel;
            model.Author = vaccination.Author;
            model.VaccinationName = vaccination.VaccinationName;
            model.VaccinationDate = vaccination.VaccinationDate;
            model.VaccinationDescription = vaccination.VaccinationDescription;
            model.Notes = vaccination.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

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
        public async Task<IActionResult> EditVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Vaccination editedVaccination = new Vaccination();
                editedVaccination.VaccinationId = model.VaccinationId;
                editedVaccination.ProgenyId = model.ProgenyId;
                editedVaccination.AccessLevel = model.AccessLevel;
                editedVaccination.Author = model.Author;
                editedVaccination.VaccinationName = model.VaccinationName;
                editedVaccination.VaccinationDate = model.VaccinationDate;
                editedVaccination.VaccinationDescription = model.VaccinationDescription;
                editedVaccination.Notes = model.Notes;

                await _vaccinationsHttpClient.UpdateVaccination(editedVaccination);
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(model.Vaccination.VaccinationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);

            return RedirectToAction("Index", "Vaccinations");
        }
    }
}