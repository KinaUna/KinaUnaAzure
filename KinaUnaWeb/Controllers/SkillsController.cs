using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class SkillsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ISkillsHttpClient _skillsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;
        public SkillsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ISkillsHttpClient skillsHttpClient, IUserAccessHttpClient userAccessHttpClient,
            IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _skillsHttpClient = skillsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            SkillsListViewModel model = new SkillsListViewModel();
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
            

            List<Skill> skillsList = await _skillsHttpClient.GetSkillsList(childId, userAccessLevel);
            skillsList = skillsList.OrderBy(s => s.SkillFirstObservation).ToList();
            if (skillsList.Count != 0)
            {
                foreach (Skill skill in skillsList)
                {
                    SkillViewModel skillViewModel = new SkillViewModel();
                    skillViewModel.ProgenyId = skill.ProgenyId;
                    skillViewModel.AccessLevel = skill.AccessLevel;
                    skillViewModel.Description = skill.Description;
                    skillViewModel.Category = skill.Category;
                    skillViewModel.Name = skill.Name;
                    skillViewModel.SkillFirstObservation = skill.SkillFirstObservation;
                    skillViewModel.SkillId = skill.SkillId;
                    skillViewModel.IsAdmin = model.IsAdmin;
                    if (skillViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.SkillsList.Add(skillViewModel);
                    }

                }
            }
            else
            {
                SkillViewModel skillViewModel = new SkillViewModel();
                skillViewModel.ProgenyId = childId;
                skillViewModel.AccessLevel = (int)AccessLevel.Public;
                skillViewModel.Description = "The skills list is empty.";
                skillViewModel.Category = "";
                skillViewModel.Name = "No items";
                skillViewModel.SkillFirstObservation = DateTime.UtcNow;

                skillViewModel.IsAdmin = model.IsAdmin;

                model.SkillsList.Add(skillViewModel);
            }

            model.Progeny = progeny;

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }


            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
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
                        if (prog.Id == model.CurrentUser.ViewChild)
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
        public async Task<IActionResult> AddSkill(SkillViewModel model)
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

            Skill skillItem = new Skill();
            skillItem.ProgenyId = model.ProgenyId;
            skillItem.Category = model.Category;
            skillItem.Description = model.Description;
            skillItem.Name = model.Name;
            skillItem.SkillAddedDate = DateTime.UtcNow;
            if (model.SkillFirstObservation == null)
            {
                model.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            skillItem.SkillFirstObservation = model.SkillFirstObservation;
            skillItem.AccessLevel = model.AccessLevel;
            skillItem.Author = model.CurrentUser.UserId;

            await _skillsHttpClient.AddSkill(skillItem);

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= skillItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy");

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = model.CurrentUser.FullName();
                        notification.Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new skill was added for " + progeny.NickName;
                        notification.Link = "/Skills?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Skill skill = await _skillsHttpClient.GetSkill(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = skill.ProgenyId;
            model.AccessLevel = skill.AccessLevel;
            model.Author = skill.Author;
            model.Category = skill.Category;
            model.Description = skill.Description;
            model.Name = skill.Name;
            model.SkillAddedDate = skill.SkillAddedDate;
            if (skill.SkillFirstObservation == null)
            {
                skill.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.SkillFirstObservation = skill.SkillFirstObservation;
            model.SkillId = skill.SkillId;
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
        public async Task<IActionResult> EditSkill(SkillViewModel model)
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
                Skill editedSkill = new Skill();

                editedSkill.ProgenyId = model.ProgenyId;
                editedSkill.AccessLevel = model.AccessLevel;
                editedSkill.Author = model.Author;
                editedSkill.Category = model.Category;
                editedSkill.Description = model.Description;
                editedSkill.Name = model.Name;
                editedSkill.SkillAddedDate = model.SkillAddedDate;
                if (model.SkillFirstObservation == null)
                {
                    model.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedSkill.SkillFirstObservation = model.SkillFirstObservation;
                editedSkill.SkillId = model.SkillId;

                await _skillsHttpClient.UpdateSkill(editedSkill);
            }

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int itemId)
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Skill = await _skillsHttpClient.GetSkill(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.Skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(SkillViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Skill skill = await _skillsHttpClient.GetSkill(model.Skill.SkillId);
            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _skillsHttpClient.DeleteSkill(skill.SkillId);

            return RedirectToAction("Index", "Skills");
        }
    }
}