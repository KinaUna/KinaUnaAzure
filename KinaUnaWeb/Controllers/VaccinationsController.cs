using System;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController(IVaccinationsHttpClient vaccinationsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        /// <summary>
        /// Vaccinations Index page. Shows a list of all vaccinations for a progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show vaccinations for.</param>
        /// <param name="vaccinationId">The Id of the Vaccination to show. If 0 no Vaccination popup is shown.</param>
        /// <returns>View with VaccinationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int vaccinationId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VaccinationViewModel model = new(baseModel);
            
            List<Vaccination> vaccinations = await vaccinationsHttpClient.GetVaccinationsList(model.CurrentProgenyId);
            model.SetVaccinationsList(vaccinations);
            model.VaccinationId = vaccinationId;

            return View(model);
        }

        /// <summary>
        /// Displays a single vaccination item.
        /// </summary>
        /// <param name="vaccinationId">The VaccinationId of the Vaccination item to display.</param>
        /// <param name="partialView">If True returns a partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with VaccinationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewVaccination(int vaccinationId, bool partialView = false)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(vaccinationId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (vaccination.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromVaccinationItem(vaccination);
            model.VaccinationItem.Progeny = model.CurrentProgeny;
            model.VaccinationItem.Progeny.PictureLink = model.VaccinationItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_VaccinationDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page for adding a new vaccination item.
        /// </summary>
        /// <returns>View with VaccinationViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            VaccinationViewModel model = new(baseModel);
            
            if (model.CurrentUser == null)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return PartialView("_AddVaccinationPartial", model);
        }

        /// <summary>
        /// HttpPost method for adding a new vaccination item.
        /// </summary>
        /// <param name="model">VaccinationViewModel with the properties of the Vaccination item to add.</param>
        /// <returns>Redirects to Vaccinations/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }
           
            model.VaccinationItem.Author = model.CurrentUser.UserId;

            model.VaccinationItem = await vaccinationsHttpClient.AddVaccination(model.VaccinationItem);
            model.VaccinationItem.VaccinationDate = TimeZoneInfo.ConvertTimeFromUtc(model.VaccinationItem.VaccinationDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VaccinationAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a vaccination item.
        /// </summary>
        /// <param name="itemId">The VaccinationId of the Vaccination item to edit.</param>
        /// <returns>View with VaccinationViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }
            
            model.SetPropertiesFromVaccinationItem(vaccination);

            model.SetAccessLevelList();

            return PartialView("_EditVaccinationPartial", model);
        }

        /// <summary>
        /// HttpPost method for editing a vaccination item.
        /// </summary>
        /// <param name="model">VaccinationViewModel with the updated properties of the Vaccination that was edited.</param>
        /// <returns>Redirects to Vaccination/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
        
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.VaccinationItem = await vaccinationsHttpClient.UpdateVaccination(model.VaccinationItem);
            model.VaccinationItem.VaccinationDate = TimeZoneInfo.ConvertTimeFromUtc(model.VaccinationItem.VaccinationDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VaccinationUpdatedPartial", model);
        }

        /// <summary>
        /// Page for deleting a vaccination item.
        /// </summary>
        /// <param name="itemId">The VaccinationId of the Vaccination item to delete.</param>
        /// <returns>View with VaccinationViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.VaccinationItem = vaccination;

            return View(model);
        }

        /// <summary>
        /// HttpPost method for deleting a vaccination item.
        /// </summary>
        /// <param name="model">VaccinationViewModel with the properties of the Vaccination item to delete.</param>
        /// <returns>Redirects to Vaccinations/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVaccination(VaccinationViewModel model)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(model.VaccinationItem.VaccinationId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            _ = await vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);

            return RedirectToAction("Index", "Vaccinations");
        }

        /// <summary>
        /// Page for copying a vaccination item.
        /// </summary>
        /// <param name="itemId">The VaccinationId of the Vaccination item to copy.</param>
        /// <returns>PartialView with VaccinationViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyVaccination(int itemId)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (model.CurrentAccessLevel > vaccination.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetPropertiesFromVaccinationItem(vaccination);

            model.SetAccessLevelList();

            return PartialView("_CopyVaccinationPartial", model);
        }

        /// <summary>
        /// HttpPost method for copying a vaccination item.
        /// </summary>
        /// <param name="model">VaccinationViewModel with the properties of the Vaccination to add.</param>
        /// <returns>PartialView with the added Vaccination item.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.VaccinationItem = await vaccinationsHttpClient.AddVaccination(model.VaccinationItem);
            model.VaccinationItem.VaccinationDate = TimeZoneInfo.ConvertTimeFromUtc(model.VaccinationItem.VaccinationDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_VaccinationCopiedPartial", model);
        }
    }
}