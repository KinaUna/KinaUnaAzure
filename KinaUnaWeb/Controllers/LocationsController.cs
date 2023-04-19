using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;
        private readonly string _hereMapsApiKey;

        public LocationsController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ILocationsHttpClient locationsHttpClient,
            IViewModelSetupService viewModelSetupService, IConfiguration configuration)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _viewModelSetupService = viewModelSetupService;
            _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };
            List<string> tagsList = new();

            List<Location> locationsList = await _locationsHttpClient.GetLocationsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            
            if (locationsList.Any())
            {
                model.LocationsList = new List<Location>();
                foreach (Location loc in locationsList)
                {
                    if (loc.AccessLevel == (int)AccessLevel.Public || loc.AccessLevel >= model.CurrentAccessLevel)
                    {
                        model.LocationsList.Add(loc);
                        if (!string.IsNullOrEmpty(loc.Tags))
                        {
                            List<string> locTags = loc.Tags.Split(',').ToList();
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
            
            model.SetTags(tagsList);
            
            if (sortBy == 1)
            {
                model.LocationsList.Reverse();
            }

            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PhotoLocations(int childId = 0, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                LocationsList = new List<Location>()
            };

            model.HereMapsApiKey = _hereMapsApiKey;

            List<string> tagsList = new();
            
            List<Picture> pictures = await _mediaHttpClient.GetPictureList(model.CurrentProgenyId, model.CurrentAccessLevel, model.CurrentUser.Timezone);
            
            if (string.IsNullOrEmpty(tagFilter))
            {
                pictures = pictures.FindAll(p => !string.IsNullOrEmpty(p.Longtitude));
            }
            else
            {
                pictures = pictures.FindAll(p =>
                    !string.IsNullOrEmpty(p.Longtitude) && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper()));
            }
            pictures = pictures.OrderBy(p => p.PictureTime).ToList();

            List<Picture> locPictures = new();
            foreach (Picture pic in pictures)
            {
                Location picLoc = new();
                bool validCoords = true;
                if (double.TryParse(pic.Latitude, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out double lat))
                {
                    picLoc.Latitude = lat;
                }
                else
                {
                    validCoords = false;
                }

                if (double.TryParse(pic.Longtitude, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out double lon))
                {
                    picLoc.Longitude = lon;
                }
                else
                {
                    validCoords = false;
                }

                if (validCoords && (pic.AccessLevel == (int)AccessLevel.Public || pic.AccessLevel >= model.CurrentAccessLevel))
                {
                    picLoc.LocationId = pic.PictureId;
                    model.LocationsList.Add(picLoc);
                    locPictures.Add(pic);
                }
            }

            if (model.LocationsList.Any())
            {
                foreach (Picture locPic in locPictures)
                {
                    if (!string.IsNullOrEmpty(locPic.Tags))
                    {
                        List<string> locTags = locPic.Tags.Split(',').ToList();
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
            
            model.SetTagList(tagsList);
            model.SetTags(tagsList);
            model.TagFilter = tagFilter;

            return View(model);
        }

        public async Task<IActionResult> AddLocation()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            List<string> tagsList = new();

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Any())
                {
                    foreach (Progeny progeny in accessList)
                    {
                        List<Location> locList1 = await _locationsHttpClient.GetLocationsList(progeny.Id, 0);
                        foreach (Location loc in locList1)
                        {
                            if (!string.IsNullOrEmpty(loc.Tags))
                            {
                                List<string> locTags = loc.Tags.Split(',').ToList();
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
            }
            
            model.SetTagList(tagsList);

            model.LocationItem.Latitude = 30.94625288456589;
            model.LocationItem.Longitude = -54.10861860580418;
            model.LocationItem.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            model.SetAccessLevelList();
            model.SetProgenyList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Location locationItem = model.CreateLocation();

            _ = await _locationsHttpClient.AddLocation(locationItem);
            
            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> EditLocation(int itemId)
        {
            Location location = await _locationsHttpClient.GetLocation(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel);
            model.HereMapsApiKey = _hereMapsApiKey;

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            List<string> tagsList = new();
            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);

                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Any())
                {
                    foreach (Progeny progeny in accessList)
                    {
                        
                        List<Location> locList1 = await _locationsHttpClient.GetLocationsList(progeny.Id, 0);
                        foreach (Location locationItem in locList1)
                        {
                            if (!string.IsNullOrEmpty(locationItem.Tags))
                            {
                                List<string> locTags = locationItem.Tags.Split(',').ToList();
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
            }
            
            model.SetPropertiesFromLocation(location, model.CurrentUser.Timezone);
            
            model.SetTagList(tagsList);
            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Location location = model.CreateLocation();

            _ = await _locationsHttpClient.UpdateLocation(location);

            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> DeleteLocation(int itemId)
        {
            Location location = await _locationsHttpClient.GetLocation(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            LocationViewModel model = new(baseModel)
            {
                LocationItem = location,
                HereMapsApiKey = _hereMapsApiKey
            };

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(LocationViewModel model)
        {
            Location location = await _locationsHttpClient.GetLocation(model.LocationItem.LocationId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await _locationsHttpClient.DeleteLocation(location.LocationId);

            return RedirectToAction("Index", "Locations");
        }
    }
}