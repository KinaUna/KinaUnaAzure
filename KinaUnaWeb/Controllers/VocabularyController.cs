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
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class VocabularyController(IWordsHttpClient wordsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        /// <summary>
        /// Vocabulary Index page. Shows a list of all vocabulary items for a progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show VocabularyItems for.</param>
        /// <param name="vocabularyId">The Id of the VocabularyItem to show. If 0 no VocabularyItem popup is shown.</param>
        /// <returns>View with VocabularyListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int vocabularyId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VocabularyListViewModel model = new(baseModel);

            List<VocabularyItem> wordList = await wordsHttpClient.GetWordsList(model.CurrentProgenyId);
            
            model.SetVocabularyList(wordList);
            
            model.SetChartData();
            model.VocabularyId = vocabularyId;

            return View(model);
        }

        /// <summary>
        /// Displays a single vocabulary item.
        /// </summary>
        /// <param name="vocabularyId">The WordId of the VocabularyItem to display.</param>
        /// <param name="partialView">If true, return a PartialView for use in popups/modals.</param>
        /// <returns>View or PartialView with VocabularyItemViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewVocabularyItem(int vocabularyId, bool partialView = false)
        {
            VocabularyItem vocabularyItem = await wordsHttpClient.GetWord(vocabularyId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            VocabularyItemViewModel model = new(baseModel);

            if (vocabularyItem.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromVocabularyItem(vocabularyItem);
            model.VocabularyItem.Progeny = model.CurrentProgeny;
            model.VocabularyItem.Progeny.PictureLink = model.VocabularyItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_VocabularyItemDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page for adding a new vocabulary item.
        /// </summary>
        /// <returns>View with VocabularyItemViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddVocabulary()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            VocabularyItemViewModel model = new(baseModel);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return PartialView("_AddVocabularyPartial", model);
        }

        /// <summary>
        /// HttpPost method for adding a new vocabulary item.
        /// </summary>
        /// <param name="model">VocabularyItemViewModel with the properties of the VocabularyItem to add.</param>
        /// <returns>Redirects to Vocabulary/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVocabulary(VocabularyItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }


            model.VocabularyItem.Date ??= TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.VocabularyItem.Author = model.CurrentUser.UserId;

            model.VocabularyItem = await wordsHttpClient.AddWord(model.VocabularyItem);
            if (model.VocabularyItem.Date.HasValue)
            {
                model.VocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.VocabularyItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.DateAdded, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VocabularyAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a vocabulary item.
        /// </summary>
        /// <param name="itemId">The WordId of the VocabularyItem to edit.</param>
        /// <returns>PartialView with VocabularyItemViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditVocabulary(int itemId)
        {
            VocabularyItem vocab = await wordsHttpClient.GetWord(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocab.ProgenyId);
            VocabularyItemViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }
            
            model.SetPropertiesFromVocabularyItem(vocab);

            model.SetAccessLevelList();

            return PartialView("_EditVocabularyPartial", model);
        }

        /// <summary>
        /// HttpPost method for editing a vocabulary item.
        /// </summary>
        /// <param name="model">VocabularyItemViewModel with the updated properties of the edited VocabularyItem.</param>
        /// <returns>PartialView with the updated VocabularyItemViewModel</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVocabulary(VocabularyItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.VocabularyItem.Date ??= TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.VocabularyItem = await wordsHttpClient.UpdateWord(model.VocabularyItem);
            if (model.VocabularyItem.Date.HasValue)
            {
                model.VocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.VocabularyItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.DateAdded, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VocabularyUpdatedPartial", model);
        }

        /// <summary>
        /// Page for deleting a vocabulary item.
        /// </summary>
        /// <param name="itemId">The WordId of the VocabularyItem to delete.</param>
        /// <returns>View with VocabularyItemViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteVocabulary(int itemId)
        {
            VocabularyItem vocabularyItem = await wordsHttpClient.GetWord(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            VocabularyItemViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.VocabularyItem = vocabularyItem;

            return View(model);
        }

        /// <summary>
        /// HttpPost method for deleting a vocabulary item.
        /// </summary>
        /// <param name="model">VocabularyItemViewModel with the properties of the VocabularyItem to delete.</param>
        /// <returns>Redirects to Vocabulary/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVocabulary(VocabularyItemViewModel model)
        {
            VocabularyItem vocabularyItem = await wordsHttpClient.GetWord(model.VocabularyItem.WordId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await wordsHttpClient.DeleteWord(vocabularyItem.WordId);

            return RedirectToAction("Index", "Vocabulary");
        }

        /// <summary>
        /// Page for copying a vocabulary item.
        /// </summary>
        /// <param name="itemId">The WordId of the VocabularyItem to copy.</param>
        /// <returns>PartialView with VocabularyItemViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyVocabulary(int itemId)
        {
            VocabularyItem vocabularyItem = await wordsHttpClient.GetWord(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vocabularyItem.ProgenyId);
            VocabularyItemViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > vocabularyItem.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetPropertiesFromVocabularyItem(vocabularyItem);

            model.SetAccessLevelList();

            return PartialView("_CopyVocabularyPartial", model);
        }

        /// <summary>
        /// HttpPost method for copying a vocabulary item.
        /// </summary>
        /// <param name="model">VocabularyItemViewModel with the properties of the copied VocabularyItem.</param>
        /// <returns>PartialView with the copied VocabularyItemViewModel</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyVocabulary(VocabularyItemViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VocabularyItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.VocabularyItem.Date ??= TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.VocabularyItem = await wordsHttpClient.AddWord(model.VocabularyItem);
            if (model.VocabularyItem.Date.HasValue)
            {
                model.VocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.VocabularyItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.VocabularyItem.DateAdded, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VocabularyCopiedPartial", model);
        }
    }
}