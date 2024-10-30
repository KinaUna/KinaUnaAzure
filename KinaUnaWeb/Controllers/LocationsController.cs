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
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;
using KinaUnaWeb.Models.TypeScriptModels.Locations;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController(
        IProgenyHttpClient progenyHttpClient,
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
        /// <param name="sortBy">The property to sort locations by, 0 = Date, 1 = Name.</param>
        /// <param name="tagFilter">Filter result to include only locations where the tagFilter is in the Tags list. Empty string includes all locations.</param>
        /// <param name="sort">Sort order, 0 = ascending, 1 descending.</param>
        /// <param name="sortTags">0 = no sorting, 1 = sort alphabetically.</param>
        /// <param name="locationId">The LocationId of the location to show details for. If 0, no Location popup is shown.</param>
        /// <returns>View with LocationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 0, string tagFilter = "", int sort = 0, int sortTags = 0, int locationId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            model.LocationsList = await locationsHttpClient.GetLocationsList(model.CurrentProgenyId, tagFilter);
            
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
            model.Sort = sort;
            model.SortTags = sortTags;

            model.LocationsPageParameters = new LocationsPageParameters
            {
                LanguageId = model.LanguageId,
                ProgenyId = model.CurrentProgenyId,
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel);

            if (location.AccessLevel < model.CurrentAccessLevel)
            {
                RedirectToAction("Index");
            }
            
            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone); 
            model.TagFilter = tagFilter;
            model.LocationItem.Progeny = model.CurrentProgeny;
            model.LocationItem.Progeny.PictureLink = model.LocationItem.Progeny.GetProfilePictureUrl();

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
                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), locationItemResponse.LocationItem.ProgenyId);
                locationItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
                locationItemResponse.LocationItem.Progeny = baseModel.CurrentProgeny;
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

            List<Location> locationsList = []; // await locationsHttpClient.GetLocationsList(parameters.ProgenyId, baseModel.CurrentAccessLevel, parameters.TagFilter);
            
            foreach (int progenyId in parameters.Progenies)
            {
                List<Location> locList = await locationsHttpClient.GetLocationsList(progenyId, parameters.TagFilter);
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
        /// <param name="tagFilter">Filter result to include only locations where the tagFilter is in the Tags list. Empty string includes all locations.</param>
        /// <param name="sortOrder">Sort order, 0 = ascending, 1 descending.</param>
        /// <returns>View with LocationViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> PhotoLocations(int childId = 0, string tagFilter = "", int sortOrder = 1)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                LocationsList = [],
                HereMapsApiKey = _hereMapsApiKey
            };

            model.LocationsPageParameters = new LocationsPageParameters
            {
                LanguageId = model.LanguageId,
                ProgenyId = model.CurrentProgenyId,
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            List<string> tagsList = [];

            if (model.CurrentUser == null)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Count != 0)
                {
                    foreach (Progeny progeny in accessList)
                    {
                        List<Location> locList1 = await locationsHttpClient.GetLocationsList(progeny.Id);
                        foreach (Location loc in locList1)
                        {
                            if (string.IsNullOrEmpty(loc.Tags)) continue;

                            List<string> locTags = [.. loc.Tags.Split(',')];
                            foreach (string tagstring in locTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                }
            }
            
            model.SetTagList(tagsList);

            model.LocationItem.Latitude = 30.94625288456589;
            model.LocationItem.Longitude = -54.10861860580418;
            model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.SetAccessLevelList();
            model.SetProgenyList();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            if (model.CurrentUser == null)
            {
                return PartialView("_AccessDeniedPartial");
            }

            List<string> tagsList = [];
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);

                List<Progeny> accessList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Count != 0)
                {
                    foreach (Progeny progeny in accessList)
                    {
                        
                        List<Location> locList1 = await locationsHttpClient.GetLocationsList(progeny.Id);
                        foreach (Location locationItem in locList1)
                        {
                            if (string.IsNullOrEmpty(locationItem.Tags)) continue;

                            List<string> locTags = [.. locationItem.Tags.Split(',')];
                            foreach (string tagstring in locTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                }
            }
            
            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone);
            
            model.SetTagList(tagsList);
            model.SetAccessLevelList();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel)
            {
                LocationItem = location,
                HereMapsApiKey = _hereMapsApiKey
            };

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                return PartialView("_AccessDeniedPartial");
            }

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            if (model.CurrentAccessLevel > location.AccessLevel)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            List<string> tagsList = [];
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);

                List<Progeny> accessList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Count != 0)
                {
                    foreach (Progeny progeny in accessList)
                    {
                        List<Location> locList1 = await locationsHttpClient.GetLocationsList(progeny.Id);
                        foreach (Location locationItem in locList1)
                        {
                            if (string.IsNullOrEmpty(locationItem.Tags)) continue;

                            List<string> locTags = [.. locationItem.Tags.Split(',')];
                            foreach (string tagstring in locTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                }
            }

            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone);

            model.SetTagList(tagsList);
            model.SetAccessLevelList();

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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
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

            return PartialView("_LocationCopiedPartial", model);
        }
    }
}