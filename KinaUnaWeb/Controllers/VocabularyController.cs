using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using System;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class VocabularyController : Controller
    {
        private readonly IWordsHttpClient _wordsHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;

        public VocabularyController(IWordsHttpClient wordsHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _wordsHttpClient = wordsHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VocabularyListViewModel model = new VocabularyListViewModel(baseModel);

            List<VocabularyItem> wordList = await _wordsHttpClient.GetWordsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            
            model.SetVocabularyList(wordList);
            
            model.SetChartData();
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddVocabulary()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            VocabularyItemViewModel model = new VocabularyItemViewModel(baseModel);
            
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
        public async Task<IActionResult> AddVocabulary(VocabularyItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }


            if (model.VocabularyItem.Date == null)
            {
                model.VocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.VocabularyItem.Author = model.CurrentUser.UserId;

            _ = await _wordsHttpClient.AddWord(model.VocabularyItem);
            
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> EditVocabulary(int itemId)
        {
            VocabularyItem vocab = await _wordsHttpClient.GetWord(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocab.ProgenyId);
            VocabularyItemViewModel model = new VocabularyItemViewModel(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            model.SetPropertiesFromVocabularyItem(vocab);

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVocabulary(VocabularyItemViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (model.VocabularyItem.Date == null)
            {
                model.VocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            await _wordsHttpClient.UpdateWord(model.VocabularyItem);

            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVocabulary(int itemId)
        {
            VocabularyItem vocabularyItem = await _wordsHttpClient.GetWord(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            VocabularyItemViewModel model = new VocabularyItemViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.VocabularyItem = vocabularyItem;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVocabulary(VocabularyItemViewModel model)
        {
            VocabularyItem vocabularyItem = await _wordsHttpClient.GetWord(model.VocabularyItem.WordId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _wordsHttpClient.DeleteWord(vocabularyItem.WordId);

            return RedirectToAction("Index", "Vocabulary");
        }
    }
}