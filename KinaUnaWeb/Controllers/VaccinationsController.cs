using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly IVaccinationsHttpClient _vaccinationsHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;

        public VaccinationsController(IVaccinationsHttpClient vaccinationsHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _vaccinationsHttpClient = vaccinationsHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            VaccinationViewModel model = new VaccinationViewModel(baseModel);
            
            List<Vaccination> vaccinations = await _vaccinationsHttpClient.GetVaccinationsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            model.SetVaccinationsList(vaccinations);
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            VaccinationViewModel model = new VaccinationViewModel(baseModel);
            
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
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
           
            model.VaccinationItem.Author = model.CurrentUser.UserId;

            _ = await _vaccinationsHttpClient.AddVaccination(model.VaccinationItem);

            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new VaccinationViewModel(baseModel);

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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.VaccinationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
        
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _vaccinationsHttpClient.UpdateVaccination(model.VaccinationItem);

            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            VaccinationViewModel model = new VaccinationViewModel(baseModel);

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
            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(model.VaccinationItem.VaccinationId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), vaccination.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await _vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);

            return RedirectToAction("Index", "Vaccinations");
        }
    }
}