using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController(IMeasurementsHttpClient measurementsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            MeasurementViewModel model = new(baseModel);
            
            model.MeasurementsList = await measurementsHttpClient.GetMeasurementsList(model.CurrentProgenyId, model.CurrentAccessLevel);
            
            if (model.MeasurementsList.Count != 0)
            {
                model.MeasurementsList = [.. model.MeasurementsList.OrderBy(m => m.Date)];
            }
            else
            {
                Measurement m = new()
                {
                    ProgenyId = childId,
                    Date = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                };
                model.MeasurementsList = [m];
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            MeasurementViewModel model = new(baseModel);
            
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
        public async Task<IActionResult> AddMeasurement(MeasurementViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.MeasurementItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.MeasurementItem.CreatedDate = DateTime.UtcNow;

            Measurement measurementItem = model.CreateMeasurement();

            _ = await measurementsHttpClient.AddMeasurement(measurementItem);
            
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int itemId)
        {
            Measurement measurement = await measurementsHttpClient.GetMeasurement(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            MeasurementViewModel model = new(baseModel);
            
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.MeasurementItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Measurement editedMeasurement = model.CreateMeasurement();

            _ = await measurementsHttpClient.UpdateMeasurement(editedMeasurement);

            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(int itemId)
        {
            Measurement measurement = await measurementsHttpClient.GetMeasurement(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            MeasurementViewModel model = new(baseModel);
           
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
            Measurement measurement = await measurementsHttpClient.GetMeasurement(model.MeasurementItem.MeasurementId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await measurementsHttpClient.DeleteMeasurement(measurement.MeasurementId);

            return RedirectToAction("Index", "Measurements");
        }
    }
}