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

namespace KinaUnaWeb.Controllers
{
    public class SleepController : Controller
    {
        private readonly ISleepHttpClient _sleepHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;

        public SleepController(ISleepHttpClient sleepHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _sleepHttpClient = sleepHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            SleepViewModel model = new SleepViewModel(baseModel);

            List<Sleep> sleepList = await _sleepHttpClient.GetSleepList(model.CurrentProgenyId, model.CurrentAccessLevel);

            model.ProcessSleepListData(sleepList);
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SleepCalendar(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            SleepViewModel model = new SleepViewModel(baseModel);
            
            List<Sleep> allSleepList = await _sleepHttpClient.GetSleepList(model.CurrentProgenyId, model.CurrentAccessLevel);
            
            model.ProcessSleepCalendarList(allSleepList);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddSleep()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            SleepViewModel model = new SleepViewModel(baseModel);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSleep(SleepViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SleepItem.Author = model.CurrentUser.UserId;
            model.SleepItem.CreatedDate = DateTime.UtcNow;

            Sleep sleepToAdd = model.CreateSleep();

            _ = await _sleepHttpClient.AddSleep(sleepToAdd);
            
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> EditSleep(int itemId)
        {
            Sleep sleep = await _sleepHttpClient.GetSleepItem(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            SleepViewModel model = new SleepViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromSleepItem(sleep);
            
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSleep(SleepViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.SleepItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                model.SleepItem.Progeny = model.CurrentProgeny;
                model.SleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                model.SleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepItem.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                if (model.SleepItem.SleepRating == 0)
                {
                    model.SleepItem.SleepRating = 3;
                }
                
                await _sleepHttpClient.UpdateSleep(model.SleepItem);
            }
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSleep(int itemId)
        {
            Sleep sleep = await _sleepHttpClient.GetSleepItem(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            SleepViewModel model = new SleepViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SleepItem = sleep;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSleep(SleepViewModel model)
        {
            Sleep sleep = await _sleepHttpClient.GetSleepItem(model.SleepItem.SleepId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), sleep.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _sleepHttpClient.DeleteSleepItem(sleep.SleepId);

            return RedirectToAction("Index", "Sleep");
        }
    }
}