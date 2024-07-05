using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController(IVaccinationsHttpClient vaccinationsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VaccinationViewModel model = new(baseModel);
            
            List<Vaccination> vaccinations = await vaccinationsHttpClient.GetVaccinationsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            model.SetVaccinationsList(vaccinations);
            
            return View(model);
        }

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

        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            VaccinationViewModel model = new(baseModel);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
           
            model.VaccinationItem.Author = model.CurrentUser.UserId;

            _ = await vaccinationsHttpClient.AddVaccination(model.VaccinationItem);

            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            model.SetPropertiesFromVaccinationItem(vaccination);

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
        
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await vaccinationsHttpClient.UpdateVaccination(model.VaccinationItem);

            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.VaccinationItem = vaccination;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVaccination(VaccinationViewModel model)
        {
            Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(model.VaccinationItem.VaccinationId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);

            return RedirectToAction("Index", "Vaccinations");
        }
    }
}