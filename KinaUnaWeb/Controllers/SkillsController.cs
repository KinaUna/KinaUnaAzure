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
            
            List<Skill> skillsList = await skillsHttpClient.GetSkillsList(model.CurrentProgenyId);
            
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
                return PartialView("_AccessDeniedPartial");
            }


            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();
            model.SetAccessLevelList();

            return PartialView("_AddSkillPartial", model);
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
                return PartialView("_AccessDeniedPartial");
            }

            Skill skillItem = model.CreateSkill();

            model.SkillItem = await skillsHttpClient.AddSkill(skillItem);
            model.SkillItem.SkillAddedDate = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillAddedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.SkillItem.SkillFirstObservation.HasValue)
            {
                model.SkillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillFirstObservation.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            return PartialView("_SkillAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a Skill item.
        /// </summary>
        /// <param name="itemId">The SkillId of the Skill to edit.</param>
        /// <returns>PartialView with SkillViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            Skill skill = await skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            skill.SkillFirstObservation ??= DateTime.UtcNow;

            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);
            
            model.SetAccessLevelList();

            return PartialView("_EditSkillPartial", model);
        }

        /// <summary>
        /// HttpPost for submitting edit Skill form.
        /// </summary>
        /// <param name="model">SkillViewModel with the updated properties of the Skill.</param>
        /// <returns>PartialView with the updated Skill.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            Skill editedSkill = model.CreateSkill();

            model.SkillItem = await skillsHttpClient.UpdateSkill(editedSkill);
            model.SkillItem.SkillAddedDate = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillAddedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.SkillItem.SkillFirstObservation.HasValue)
            {
                model.SkillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillFirstObservation.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            return PartialView("_SkillUpdatedPartial", model);
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
                return PartialView("_AccessDeniedPartial");
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
                return PartialView("_AccessDeniedPartial");
            }

            _ = await skillsHttpClient.DeleteSkill(skill.SkillId);

            return RedirectToAction("Index", "Skills");
        }

        /// <summary>
        /// Page for copying a Skill item.
        /// </summary>
        /// <param name="itemId">The SkillId of the Skill to copy.</param>
        /// <returns>PartialView with SkillViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopySkill(int itemId)
        {
            Skill skill = await skillsHttpClient.GetSkill(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), skill.ProgenyId);
            SkillViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > skill.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();
            
            skill.SkillFirstObservation ??= DateTime.UtcNow;

            model.SetPropertiesFromSkillItem(skill, model.IsCurrentUserProgenyAdmin);

            model.SetAccessLevelList();

            return PartialView("_CopySkillPartial", model);
        }

        /// <summary>
        /// HttpPost for submitting copy Skill form.
        /// </summary>
        /// <param name="model">SkillViewModel with the properties of the Skill to add.</param>
        /// <returns>PartialView with the added Skill.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopySkill(SkillViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SkillItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            Skill editedSkill = model.CreateSkill();

            model.SkillItem = await skillsHttpClient.AddSkill(editedSkill);
            model.SkillItem.SkillAddedDate = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillAddedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.SkillItem.SkillFirstObservation.HasValue)
            {
                model.SkillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(model.SkillItem.SkillFirstObservation.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            return PartialView("_SkillCopiedPartial", model);
        }
    }
}