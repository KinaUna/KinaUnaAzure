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
        /// <summary>
        /// Measurement Index page. Shows a list of all measurements for a Progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show Measurements for.</param>
        /// <returns>View with MeasurementViewModel.</returns>
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

        /// <summary>
        /// Page or Partial view to show a single Measurement.
        /// </summary>
        /// <param name="measurementId">The MeasurementId of the Measurement to view.</param>
        /// <param name="partialView">Return partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with MeasurementViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewMeasurement(int measurementId, bool partialView = false)
        {
            Measurement measurement = await measurementsHttpClient.GetMeasurement(measurementId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), measurement.ProgenyId);
            MeasurementViewModel model = new(baseModel);

            if (measurement.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromMeasurement(measurement, model.IsCurrentUserProgenyAdmin);


           
            model.MeasurementItem.Progeny = model.CurrentProgeny;
            model.MeasurementItem.Progeny.PictureLink = model.MeasurementItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_MeasurementDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Page to add a new Measurement.
        /// </summary>
        /// <returns>View with MeasurementViewModel.</returns>
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

        /// <summary>
        /// HttpPost endpoint for adding a new measurement.
        /// </summary>
        /// <param name="model">MeasurementViewModel with the properties of the Measurement to add.</param>
        /// <returns>Redirects to Measurements/Index page.</returns>
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

        /// <summary>
        /// Page to edit a Measurement.
        /// </summary>
        /// <param name="itemId">The MeasurementId of the Measurement to edit.</param>
        /// <returns>View with MeasurementViewModel.</returns>
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

        /// <summary>
        /// HttpPost endpoint for editing a Measurement.
        /// </summary>
        /// <param name="model">MeasurementViewModel with the updated properties of the Measurement that was edited.</param>
        /// <returns>Redirects to Measurements/Index page.</returns>
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

        /// <summary>
        /// Page to delete a Measurement.
        /// </summary>
        /// <param name="itemId">The MeasurementId of the Measurement to delete.</param>
        /// <returns>View with MeasurementViewModel.</returns>
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

        /// <summary>
        /// HttpPost endpoint for deleting a Measurement.
        /// </summary>
        /// <param name="model">MeasurementViewModel with the properties for the Measurement to delete.</param>
        /// <returns>Redirects to Measurements/Index page.</returns>
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