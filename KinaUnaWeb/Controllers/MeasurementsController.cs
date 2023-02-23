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

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController : Controller
    {
        private readonly IMeasurementsHttpClient _measurementsHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;

        public MeasurementsController(IMeasurementsHttpClient measurementsHttpClient, IViewModelSetupService viewModelSetupService)
        {
            _measurementsHttpClient = measurementsHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            MeasurementViewModel model = new MeasurementViewModel(baseModel);
            
            model.MeasurementsList = await _measurementsHttpClient.GetMeasurementsList(childId, model.CurrentAccessLevel);
            
            if (model.MeasurementsList.Count != 0)
            {
                model.MeasurementsList = model.MeasurementsList.OrderBy(m => m.Date).ToList();
            }
            else
            {
                Measurement m = new Measurement();
                m.ProgenyId = childId;
                m.Date = DateTime.UtcNow;
                m.CreatedDate = DateTime.UtcNow;
                model.MeasurementsList = new List<Measurement>();
                model.MeasurementsList.Add(m);
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            MeasurementViewModel model = new MeasurementViewModel(baseModel);
            
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
        public async Task<IActionResult> AddMeasurement(MeasurementViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.MeasurementItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.MeasurementItem.CreatedDate = DateTime.UtcNow;

            Measurement measurementItem = model.CreateMeasurement();

            _ = await _measurementsHttpClient.AddMeasurement(measurementItem);
            
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int itemId)
        {
            Measurement measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            MeasurementViewModel model = new MeasurementViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromMeasurement(measurement, model.IsCurrentUserProgenyAdmin);

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeasurement(MeasurementViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.MeasurementItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Measurement editedMeasurement = model.CreateMeasurement();

            _ = await _measurementsHttpClient.UpdateMeasurement(editedMeasurement);

            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(int itemId)
        {
            Measurement measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            MeasurementViewModel model = new MeasurementViewModel(baseModel);
           
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.MeasurementItem = measurement;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(MeasurementViewModel model)
        {
            Measurement measurement = await _measurementsHttpClient.GetMeasurement(model.MeasurementItem.MeasurementId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await _measurementsHttpClient.DeleteMeasurement(measurement.MeasurementId);

            return RedirectToAction("Index", "Measurements");
        }
    }
}