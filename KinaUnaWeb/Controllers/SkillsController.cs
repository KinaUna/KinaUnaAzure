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
    public class SkillsController(ISkillsHttpClient skillsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        /// <summary>
        /// Skills Index page. Shows a list of all skills for a progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show Skills for.</param>
        /// <param name="skillId">The Id of the Skill to show. If 0 no Skill popup is shown.</param>
        /// <returns>View with SkillsListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int skillId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            SkillsListViewModel model = new(baseModel);
            
            List<Skill> skillsList = await skillsHttpClient.GetSkillsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            
            if (skillsList.Count != 0)
            {
                skillsList = [.. skillsList.OrderBy(s => s.SkillFirstObservation)];
                
                foreach (Skill skill in skillsList)
                {
                    if (skill.AccessLevel < model.CurrentAccessLevel) continue;

                    SkillViewModel skillViewModel = new(baseModel);
                    skillViewModel.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
                    model.SkillsList.Add(skillViewModel);

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

            model.SkillId = skillId;

            return View(model);

        }

        /// <summary>
        /// View a single Skill item.
        /// </summary>
        /// <param name="skillId">The SkillId of the Skill to view.</param>
        /// <param name="partialView">If True returns a partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with SkillViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewSkill(int skillId, bool partialView = false)
        {
            Skill skill = await skillsHttpClient.GetSkill(skillId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);

            if (skill.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
            model.SkillItem.Progeny = model.CurrentProgeny;
            model.SkillItem.Progeny.PictureLink = model.SkillItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_SkillDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page for adding a new Skill item.
        /// </summary>
        /// <returns>View with SkillViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            SkillViewModel model = new(baseModel);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }


            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();
            model.SetAccessLevelList();

            return View(model);
        }

        /// <summary>
        /// HttpPost for submitting new Skill form.
        /// </summary>
        /// <param name="model">SkillViewModel with the properties for the Skill to add.</param>
        /// <returns>Redirects to Skills/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill skillItem = model.CreateSkill();

            _ = await skillsHttpClient.AddSkill(skillItem);
            
            return RedirectToAction("Index", "Skills");
        }

        /// <summary>
        /// Page for editing a Skill item.
        /// </summary>
        /// <param name="itemId">The SkillId of the Skill to edit.</param>
        /// <returns>View with SkillViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            Skill skill = await skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            skill.SkillFirstObservation ??= DateTime.UtcNow;

            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
            
            model.SetAccessLevelList();

            return View(model);
        }

        /// <summary>
        /// HttpPost for submitting edit Skill form.
        /// </summary>
        /// <param name="model">SkillViewModel with the updated properties of the Skill.</param>
        /// <returns>Redirects to Skills/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill editedSkill = model.CreateSkill();

            _ = await skillsHttpClient.UpdateSkill(editedSkill);

            return RedirectToAction("Index", "Skills");
        }

        /// <summary>
        /// Page for deleting a Skill item.
        /// </summary>
        /// <param name="itemId">The SkillId of the Skill to delete.</param>
        /// <returns>View with SkillViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int itemId)
        {
            Skill skill = await skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);
            
            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        /// <summary>
        /// HttpPost for deleting a Skill item.
        /// </summary>
        /// <param name="model">SkillViewModel with the properties of the Skill to delete.</param>
        /// <returns>Redirects to Skills/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(SkillViewModel model)
        {
            Skill skill = await skillsHttpClient.GetSkill(model.SkillItem.SkillId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await skillsHttpClient.DeleteSkill(skill.SkillId);

            return RedirectToAction("Index", "Skills");
        }
    }
}