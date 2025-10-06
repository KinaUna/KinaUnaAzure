using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class SleepController(ISleepHttpClient sleepHttpClient, IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient) : Controller
    {
        /// <summary>
        /// Sleep Index page. Shows a list of all sleep items for a progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show sleep data for.</param>
        /// <param name="sleepId">The Id of the Sleep item to show. If 0 no Sleep popup is shown.</param>
        /// <returns>View with SleepViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sleepId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, 0, true);
            SleepViewModel model = new(baseModel);

            List<Sleep> sleepList = await sleepHttpClient.GetSleepList(model.CurrentProgenyId);

            model.ProcessSleepListData(sleepList);
            
            model.SleepId = sleepId;

            return View(model);
        }

        /// <summary>
        /// Displays a single sleep item.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep item to display.</param>
        /// <param name="partialView">If True returns a partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with SleepViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewSleep(int sleepId, bool partialView = false)
        {
            Sleep sleepItem = await sleepHttpClient.GetSleepItem(sleepId);
            if (sleepItem == null || sleepItem.SleepId == 0)
            {
                if (partialView)
                {
                    return PartialView("_NotFoundPartial");
                }
                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleepItem.ProgenyId, 0, false);
            SleepViewModel model = new(baseModel);
            
            model.SetPropertiesFromSleepItem(sleepItem);
            model.SleepItem.Progeny = model.CurrentProgeny;
            model.SleepItem.Progeny.PictureLink = model.SleepItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_SleepDetailsPartial", model);
            }
            return View(model);

        }

        /// <summary>
        /// Sleep Calendar page. Shows a calendar with sleep items for a progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show sleep data for.</param>
        /// <returns>View with SleepViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> SleepCalendar(int childId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, 0, false);
            SleepViewModel model = new(baseModel);
            
            List<Sleep> allSleepList = await sleepHttpClient.GetSleepList(model.CurrentProgenyId);
            
            model.ProcessSleepCalendarList(allSleepList);

            return View(model);
        }

        /// <summary>
        /// Page for adding a new sleep item.
        /// </summary>
        /// <returns>View with SleepViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddSleep()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            SleepViewModel model = new(baseModel);
            
            if (model.CurrentUser == null)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();

            return PartialView("_AddSleepPartial", model);
        }

        /// <summary>
        /// HttpPost for submitting new sleep form.
        /// </summary>
        /// <param name="model">SleepViewModel with the properties of the Sleep item to add.</param>
        /// <returns>Redirects to Sleep/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSleep(SleepViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.SleepItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.SleepItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add sleep for.
                return RedirectToAction("Index");
            }

            model.SleepItem.Author = model.CurrentUser.UserId;
            model.SleepItem.CreatedDate = DateTime.UtcNow;

            Sleep sleepToAdd = model.CreateSleep();

            model.SleepItem = await sleepHttpClient.AddSleep(sleepToAdd);
            model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_SleepAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a sleep item.
        /// </summary>
        /// <param name="itemId">The SleepId of the Sleep item to edit.</param>
        /// <returns>View with SleepViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditSleep(int itemId)
        {
            Sleep sleep = await sleepHttpClient.GetSleepItem(itemId);
            if (sleep == null || sleep.SleepId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (sleep.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId, 0, false);
            SleepViewModel model = new(baseModel);
            
            model.SetPropertiesFromSleepItem(sleep);
            model.SetRatingList();
            
            return PartialView("_EditSleepPartial", model);
        }

        /// <summary>
        /// HttpPost for submitting edit sleep form.
        /// </summary>
        /// <param name="model">SleepViewModel with the updated properties of the Sleep item.</param>
        /// <returns>Sleep updated page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSleep(SleepViewModel model)
        {
            Sleep existingSleep = await sleepHttpClient.GetSleepItem(model.SleepItem.SleepId);
            if (existingSleep == null || existingSleep.SleepId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (existingSleep.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            
            model.SleepItem.Progeny = model.CurrentProgeny;
            model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.SleepItem.SleepRating == 0)
            {
                model.SleepItem.SleepRating = 3;
            }
                
            model.SleepItem = await sleepHttpClient.UpdateSleep(model.SleepItem);
            model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_SleepUpdatedPartial", model);
        }

        /// <summary>
        /// Page for deleting a sleep item.
        /// </summary>
        /// <param name="itemId">The SleepId of the Sleep item to delete.</param>
        /// <returns>View with SleepViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteSleep(int itemId)
        {
            Sleep sleep = await sleepHttpClient.GetSleepItem(itemId);
            if (sleep == null || sleep.SleepId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (sleep.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId, 0, false);
            SleepViewModel model = new(baseModel);
            
            model.SleepItem = sleep;

            return View(model);
        }

        /// <summary>
        /// HttpPost for form to delete a sleep item.
        /// </summary>
        /// <param name="model">SleepViewModel with the properties for the Sleep item to delete.</param>
        /// <returns>Redirects to Sleep/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSleep(SleepViewModel model)
        {
            Sleep sleep = await sleepHttpClient.GetSleepItem(model.SleepItem.SleepId);
            if (sleep == null || sleep.SleepId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (sleep.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);

            await sleepHttpClient.DeleteSleepItem(sleep.SleepId);

            return RedirectToAction("Index", "Sleep");
        }

        /// <summary>
        /// Page for copying a sleep item.
        /// </summary>
        /// <param name="itemId">The SleepId of the Sleep item to copy.</param>
        /// <returns>PartialView with SleepViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopySleep(int itemId)
        {
            Sleep sleep = await sleepHttpClient.GetSleepItem(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            SleepViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();

            model.SetPropertiesFromSleepItem(sleep);
            model.SetRatingList();
            
            return PartialView("_CopySleepPartial", model);
        }

        /// <summary>
        /// HttpPost for submitting copy sleep form.
        /// </summary>
        /// <param name="model">SleepViewModel with the updated properties of the Sleep item.</param>
        /// <returns>SleepCopied PartialView.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopySleep(SleepViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            bool canUserAdd = false;
            if (model.SleepItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.SleepItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add sleep for.
                return RedirectToAction("Index");
            }

            model.SleepItem.Progeny = model.CurrentProgeny;
            model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            if (model.SleepItem.SleepRating == 0)
            {
                model.SleepItem.SleepRating = 3;
            }

            model.SleepItem = await sleepHttpClient.UpdateSleep(model.SleepItem);
            model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_SleepCopiedPartial", model);
        }
    }
}