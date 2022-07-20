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

namespace KinaUnaWeb.Controllers
{
    public class SkillsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ISkillsHttpClient _skillsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public SkillsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, ISkillsHttpClient skillsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _skillsHttpClient = skillsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
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
    }
}