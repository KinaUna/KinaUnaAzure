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
    public class LocationsController(
        IProgenyHttpClient progenyHttpClient,
        IMediaHttpClient mediaHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IConfiguration configuration)
        : Controller
    {
        private readonly string _hereMapsApiKey = configuration.GetValue<string>("HereMapsKey");

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };
            List<string> tagsList = [];

            List<Location> locationsList = await locationsHttpClient.GetLocationsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            
            if (locationsList.Count != 0)
            {
                model.LocationsList = [];
                foreach (Location loc in locationsList)
                {
                    if (loc.AccessLevel != (int)AccessLevel.Public && loc.AccessLevel < model.CurrentAccessLevel) continue;

                    model.LocationsList.Add(loc);
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                LocationsList = [],
                HereMapsApiKey = _hereMapsApiKey
            };

            List<string> tagsList = [];
            
            List<Picture> pictures = await mediaHttpClient.GetPictureList(model.CurrentProgenyId, model.CurrentAccessLevel, model.CurrentUser.Timezone);
            
            if (string.IsNullOrEmpty(tagFilter))
            {
                pictures = pictures.FindAll(p => !string.IsNullOrEmpty(p.Longtitude));
            }
            else
            {
                pictures = pictures.FindAll(p =>
                    !string.IsNullOrEmpty(p.Longtitude) && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase));
            }
            pictures = [.. pictures.OrderBy(p => p.PictureTime)];

            List<Picture> locPictures = [];
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

                if (!validCoords || (pic.AccessLevel != (int)AccessLevel.Public && pic.AccessLevel < model.CurrentAccessLevel)) continue;

                picLoc.LocationId = pic.PictureId;
                model.LocationsList.Add(picLoc);
                locPictures.Add(pic);
            }

            if (model.LocationsList.Count != 0)
            {
                foreach (Picture locPic in locPictures)
                {
                    if (string.IsNullOrEmpty(locPic.Tags)) continue;

                    List<string> locTags = [.. locPic.Tags.Split(',')];
                    foreach (string tagstring in locTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
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
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            List<string> tagsList = [];

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
                if (accessList.Count != 0)
                {
                    foreach (Progeny progeny in accessList)
                    {
                        List<Location> locList1 = await locationsHttpClient.GetLocationsList(progeny.Id, 0);
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Location locationItem = model.CreateLocation();

            _ = await locationsHttpClient.AddLocation(locationItem);
            
            return RedirectToAction("Index", "Locations");
        }

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
                return RedirectToAction("Index");
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
                        
                        List<Location> locList1 = await locationsHttpClient.GetLocationsList(progeny.Id, 0);
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.LocationItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Location location = model.CreateLocation();

            _ = await locationsHttpClient.UpdateLocation(location);

            return RedirectToAction("Index", "Locations");
        }

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
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(LocationViewModel model)
        {
            Location location = await locationsHttpClient.GetLocation(model.LocationItem.LocationId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), location.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await locationsHttpClient.DeleteLocation(location.LocationId);

            return RedirectToAction("Index", "Locations");
        }
    }
}