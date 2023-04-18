using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class SkillsController : Controller
    {
        private readonly ISkillsHttpClient _skillsHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;
        public SkillsController(ISkillsHttpClient skillsHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _skillsHttpClient = skillsHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            SkillsListViewModel model = new(baseModel);
            
            List<Skill> skillsList = await _skillsHttpClient.GetSkillsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            
            if (skillsList.Count != 0)
            {
                skillsList = skillsList.OrderBy(s => s.SkillFirstObservation).ToList();
                
                foreach (Skill skill in skillsList)
                {
                    if (skill.AccessLevel >= model.CurrentAccessLevel)
                    {
                        SkillViewModel skillViewModel = new(baseModel);
                        skillViewModel.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
                        model.SkillsList.Add(skillViewModel);
                    }

                }
            }
            else
            {
                SkillViewModel skillViewModel = new()
                {
                    SkillItem =
                    {
                        ProgenyId = childId,
                        AccessLevel = (int)AccessLevel.Public,
                        Description = "The skills list is empty.",
                        Category = "",
                        Name = "No items",
                        SkillFirstObservation = DateTime.UtcNow
                    },
                    IsCurrentUserProgenyAdmin = model.IsCurrentUserProgenyAdmin
                };

                model.SkillsList.Add(skillViewModel);
            }
            
            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            SkillViewModel model = new(baseModel);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }


            model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill skillItem = model.CreateSkill();

            _ = await _skillsHttpClient.AddSkill(skillItem);
            
            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            Skill skill = await _skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (skill.SkillFirstObservation == null)
            {
                skill.SkillFirstObservation = DateTime.UtcNow; 
            }
            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
            
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill editedSkill = model.CreateSkill();

            _ = await _skillsHttpClient.UpdateSkill(editedSkill);

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int itemId)
        {
            Skill skill = await _skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);
            
            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
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
            Skill skill = await _skillsHttpClient.GetSkill(model.SkillItem.SkillId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await _skillsHttpClient.DeleteSkill(skill.SkillId);

            return RedirectToAction("Index", "Skills");
        }
    }
}