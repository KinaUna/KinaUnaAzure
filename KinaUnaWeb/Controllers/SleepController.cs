using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class SleepController(ISleepHttpClient sleepHttpClient, IViewModelSetupService viewModelSetupService) : Controller
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleepItem.ProgenyId);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
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

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

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
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            SleepViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.SetPropertiesFromSleepItem(sleep);
            model.SetRatingList();
            model.SetAccessLevelList();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            if (!ModelState.IsValid) return RedirectToAction("Index", "Sleep");

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            SleepViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

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

            if (model.CurrentAccessLevel > sleep.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetPropertiesFromSleepItem(sleep);
            model.SetRatingList();
            model.SetAccessLevelList();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            if (!ModelState.IsValid) return RedirectToAction("Index", "Sleep");

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