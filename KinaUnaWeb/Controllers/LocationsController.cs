using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.Locations;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController(
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IConfiguration configuration)
        : Controller
    {
        private readonly string _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");

        /// <summary>
        /// Locations Index page. Shows a list of all locations for a Progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show locations for.</param>
        /// <param name="familyId">The Id of the Family to show locations for.</param>
        /// <param name="sortBy">The property to sort locations by, 0 = Date, 1 = Name.</param>
        /// <param name="tagFilter">Filter result to include only locations where the tagFilter is in the Tags list. Empty string includes all locations.</param>
        /// <param name="sort">Sort order, 0 = ascending, 1 descending.</param>
        /// <param name="sortTags">0 = no sorting, 1 = sort alphabetically.</param>
        /// <param name="locationId">The LocationId of the location to show details for. If 0, no Location popup is shown.</param>
        /// <returns>View with LocationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int familyId = 0, int sortBy = 0, string tagFilter = "", int sort = 0, int sortTags = 0, int locationId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };
            // Todo: Replace with lists of progenies and families.
            model.LocationsList = await locationsHttpClient.GetLocationsList(model.CurrentProgenyId, model.CurrentFamilyId, tagFilter);
            
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
            model.Sort = sort;
            model.SortTags = sortTags;

            model.LocationsPageParameters = new LocationsPageParameters
            {
                LanguageId = model.LanguageId,
                ProgenyId = model.CurrentProgenyId,
                FamilyId = model.CurrentFamilyId,
                SortBy = sortBy,
                TagFilter = tagFilter,
                Sort = sort,
                SortTags = sortTags
            };

            model.LocationId = locationId;

            return View(model);
        }

        /// <summary>
        /// Page or Partial view to show details for a location.
        /// </summary>
        /// <param name="locationId">The LocationId of the location to show.</param>
        /// <param name="tagFilter">Active tag filter.</param>
        /// <param name="partialView">Return partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with LocationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewLocation(int locationId, string tagFilter, bool partialView = false)
        {
            Location location = await locationsHttpClient.GetLocation(locationId);
            if (location == null || location.LocationId == 0)
            {
                if (partialView)
                {
                    return PartialView("_NotFoundPartial");
                }

                return NotFound();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel);
            
            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone); 
            model.TagFilter = tagFilter;
            if (model.LocationItem.ProgenyId > 0)
            {
                model.LocationItem.Progeny = model.CurrentProgeny;
                model.LocationItem.Progeny.PictureLink = model.LocationItem.Progeny.GetProfilePictureUrl();
            }
            if (model.LocationItem.FamilyId > 0)
            {
                model.LocationItem.Family = await familiesHttpClient.GetFamily(model.LocationItem.FamilyId);
                if (model.LocationItem.Family != null)
                {
                    model.LocationItem.Family.PictureLink = model.LocationItem.Family.GetProfilePictureUrl();
                }
            }

            if (partialView)
            {
                return PartialView("_LocationDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// Gets a partial view with a Location element, for contact lists to fetch HTML for each Location.
        /// </summary>
        /// <param name="parameters">LocationItemParameters object with the Location details.</param>
        /// <returns>PartialView with LocationViewModel.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LocationElement([FromBody] LocationItemParameters parameters)
        {
            parameters ??= new LocationItemParameters();

            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            LocationViewModel locationItemResponse = new()
            {
                LanguageId = parameters.LanguageId
            };

            if (parameters.LocationId == 0)
            {
                locationItemResponse.LocationItem = new Location { LocationId = 0 };
            }
            else
            {
                locationItemResponse.LocationItem = await locationsHttpClient.GetLocation(parameters.LocationId);
                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), locationItemResponse.LocationItem.ProgenyId, locationItemResponse.LocationItem.FamilyId, false);
                if (locationItemResponse.LocationItem.ProgenyId > 0)
                {
                    locationItemResponse.LocationItem.Progeny = baseModel.CurrentProgeny;
                }
                if (locationItemResponse.LocationItem.FamilyId > 0)
                {
                    locationItemResponse.LocationItem.Family = await familiesHttpClient.GetFamily(locationItemResponse.LocationItem.FamilyId);
                }
            }

            return PartialView("_LocationElementPartial", locationItemResponse);
        }

        /// <summary>
        /// HttpPost endpoint for fetching a list of Locations.
        /// </summary>
        /// <param name="parameters">LocationsPageParameters object.</param>
        /// <returns>Json of LocationsPageResponse</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LocationsList([FromBody] LocationsPageParameters parameters)
        {
            parameters ??= new LocationsPageParameters();

            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            List<Location> locationsList = [];
            
            foreach (int progenyId in parameters.Progenies)
            {
                List<Location> locList = await locationsHttpClient.GetLocationsList(progenyId, 0, parameters.TagFilter);
                locationsList.AddRange(locList);
            }
            
            List<string> tagsList = [];

            if (locationsList.Count != 0)
            {
                foreach (Location location in locationsList)
                {
                    if (!string.IsNullOrEmpty(location.Tags))
                    {
                        List<string> locationTagsList = [.. location.Tags.Split(',')];
                        foreach (string tagString in locationTagsList)
                        {
                            string trimmedTagString = tagString.TrimStart(' ', ',').TrimEnd(' ', ',');
                            if (!string.IsNullOrEmpty(trimmedTagString) && !tagsList.Contains(trimmedTagString))
                            {
                                tagsList.Add(trimmedTagString);
                            }
                        }
                    }
                }
            }

            if (parameters.SortTags == 1)
            {
                tagsList = [.. tagsList.OrderBy(t => t)];
            }

            if (parameters.SortBy == 0)
            {
                locationsList = [.. locationsList.OrderBy(l => l.Date)];
            }
            if (parameters.SortBy == 1)
            {
                locationsList = [.. locationsList.OrderBy(f => f.Name)];
            }
            
            if (parameters.Sort == 1)
            {
                locationsList.Reverse();
            }

            // List<int> locationsIdList = locationsList.Select(l => l.LocationId).ToList();

            return Json(new LocationsPageResponse()
            {
                LocationsList = locationsList,
                PageNumber = parameters.CurrentPageNumber,
                TotalItems = locationsList.Count,
                TagsList = tagsList
            });
        }

        /// <summary>
        /// Page to show a map with all locations obtained from photos for a Progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show photo locations for.</param>
        /// <param name="familyId">The Id of the Family to show photo locations for.</param>
        /// <param name="tagFilter">Filter result to include only locations where the tagFilter is in the Tags list. Empty string includes all locations.</param>
        /// <param name="sortOrder">Sort order, 0 = ascending, 1 descending.</param>
        /// <returns>View with LocationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> PhotoLocations(int childId = 0, int familyId = 0, string tagFilter = "", int sortOrder = 1)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, familyId);
            LocationViewModel model = new(baseModel)
            {
                LocationsList = [],
                HereMapsApiKey = _hereMapsApiKey
            };

            model.LocationsPageParameters = new LocationsPageParameters
            {
                LanguageId = model.LanguageId,
                ProgenyId = model.CurrentProgenyId,
                FamilyId = model.CurrentFamilyId,
                TagFilter = tagFilter,
                Sort = sortOrder
            };

            return View(model);
        }

        /// <summary>
        /// Page to add a new location.
        /// </summary>
        /// <returns>View with LocationViewModel.</returns>
        public async Task<IActionResult> AddLocation()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList();
            model.SetFamilyList();

            model.LocationItem.Latitude = 30.94625288456589;
            model.LocationItem.Longitude = -54.10861860580418;
            model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            return PartialView("_AddLocationPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for adding a new location.
        /// </summary>
        /// <param name="model">LocationViewModel with the properties of the Location to add.</param>
        /// <returns>Redirects to Locations/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId, model.LocationItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.LocationItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.LocationItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.LocationItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.LocationItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add kanban for.
                return RedirectToAction("Index");
            }

            Location locationItem = model.CreateLocation();

            model.LocationItem = await locationsHttpClient.AddLocation(locationItem);
            if (model.LocationItem.Date.HasValue)
            {
                model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.LocationItem.DateAdded.HasValue)
            {
                model.LocationItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.LocationItem.ProgenyId > 0)
            {
                model.LocationItem.Progeny = model.CurrentProgeny;
            }

            if (model.LocationItem.FamilyId > 0)
            {
                model.LocationItem.Family = model.CurrentFamily;
            }

            return PartialView("_LocationAddedPartial", model);
        }

        /// <summary>
        /// Page to edit a location.
        /// </summary>
        /// <param name="itemId">The LocationId of the Location to edit.</param>
        /// <returns>View with LocationViewModel.</returns>
        public async Task<IActionResult> EditLocation(int itemId)
        {
            Location location = await locationsHttpClient.GetLocation(itemId);
            if (location == null || location.LocationId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (location.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId, location.FamilyId, false);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(location.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(location.FamilyId);
            model.SetFamilyList();

            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone);
            
            return PartialView("_EditLocationPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for editing a location.
        /// </summary>
        /// <param name="model">LocationViewModel with the updated properties of the Location.</param>
        /// <returns>Redirects to Locations/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            Location existingLocation = await locationsHttpClient.GetLocation(model.LocationItem.LocationId);
            if (existingLocation == null || existingLocation.LocationId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (existingLocation.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId, model.LocationItem.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            Location location = model.CreateLocation();

            model.LocationItem = await locationsHttpClient.UpdateLocation(location);
            if (model.LocationItem.Date.HasValue)
            {
                model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            } 
            if (model.LocationItem.DateAdded.HasValue)
            {
                model.LocationItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.LocationItem.ProgenyId > 0)
            {
                model.LocationItem.Progeny = model.CurrentProgeny;
            }

            if (model.LocationItem.FamilyId > 0)
            {
                model.LocationItem.Family = model.CurrentFamily;
            }

            return PartialView("_LocationUpdatedPartial", model);
        }

        /// <summary>
        /// Page to delete a location.
        /// </summary>
        /// <param name="itemId">The LocationId of the Location to delete.</param>
        /// <returns>View with LocationViewModel.</returns>
        public async Task<IActionResult> DeleteLocation(int itemId)
        {
            Location location = await locationsHttpClient.GetLocation(itemId);
            if (location == null || location.LocationId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (location.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId, location.FamilyId, false);
            LocationViewModel model = new(baseModel)
            {
                LocationItem = location,
                HereMapsApiKey = _hereMapsApiKey
            };

            if (model.LocationItem.ProgenyId > 0)
            {
                model.LocationItem.Progeny = model.CurrentProgeny;
            }

            if (model.LocationItem.FamilyId > 0)
            {
                model.LocationItem.Family = model.CurrentFamily;
            }

            return View(model);
        }

        /// <summary>
        /// HttpPost endpoint for deleting a location.
        /// </summary>
        /// <param name="model">LocationViewModel with the properties of the Location to delete.</param>
        /// <returns>Redirects to Locations/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(LocationViewModel model)
        {
            Location location = await locationsHttpClient.GetLocation(model.LocationItem.LocationId);
            if (location == null || location.LocationId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (location.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId, location.FamilyId, false);
            model.SetBaseProperties(baseModel);
            
            _ = await locationsHttpClient.DeleteLocation(location.LocationId);

            return RedirectToAction("Index", "Locations");
        }

        /// <summary>
        /// Page to copy a location.
        /// </summary>
        /// <param name="itemId">The LocationId of the Location to copy.</param>
        /// <returns>PartialView with LocationViewModel.</returns>
        public async Task<IActionResult> CopyLocation(int itemId)
        {
            Location location = await locationsHttpClient.GetLocation(itemId);
            if (location == null || location.LocationId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId, location.FamilyId, false);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(location.ProgenyId);
            model.SetProgenyList();
            model.FamilyList = await viewModelSetupService.GetFamilySelectList(location.FamilyId);
            model.SetFamilyList();
            
            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone);
            
            return PartialView("_CopyLocationPartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for copying a location.
        /// </summary>
        /// <param name="model">LocationViewModel with the properties of the Location to add.</param>
        /// <returns>PartialView with the added Location item.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId, model.LocationItem.FamilyId, false);
            model.SetBaseProperties(baseModel);

            bool canUserAdd = false;
            if (model.LocationItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.LocationItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (model.LocationItem.FamilyId > 0)
            {
                List<Family> families = await familiesHttpClient.GetFamiliesUserCanAccess(PermissionLevel.Add);
                if (families.Exists(f => f.FamilyId == model.LocationItem.FamilyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no family or family members are available to add kanban for.
                return PartialView("_AccessDeniedPartial");
            }


            Location location = model.CreateLocation();

            model.LocationItem = await locationsHttpClient.AddLocation(location);
            
            if (model.LocationItem.Date.HasValue)
            {
                model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (model.LocationItem.DateAdded.HasValue)
            {
                model.LocationItem.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(model.LocationItem.DateAdded.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (model.LocationItem.ProgenyId > 0)
            {
                model.LocationItem.Progeny = model.CurrentProgeny;
            }

            if (model.LocationItem.FamilyId > 0)
            {
                model.LocationItem.Family = model.CurrentFamily;
            }

            return PartialView("_LocationCopiedPartial", model);
        }
    }
}