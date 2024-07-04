﻿using KinaUnaWeb.Models.ItemViewModels;
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
using KinaUnaWeb.Models.TypeScriptModels.Locations;

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
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 0, string tagFilter = "", int sort = 0, int sortTags = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            LocationViewModel model = new(baseModel)
            {
                HereMapsApiKey = _hereMapsApiKey
            };

            model.LocationsList = await locationsHttpClient.GetLocationsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            
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

            return View(model);
        }

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
            }


            return PartialView("_LocationElementPartial", locationItemResponse);

        }

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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), parameters.ProgenyId);
            List<Location> locationsList = await locationsHttpClient.GetLocationsList(parameters.ProgenyId, baseModel.CurrentAccessLevel, parameters.TagFilter);

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

            List<int> locationsIdList = locationsList.Select(l => l.LocationId).ToList();

            return Json(new LocationsPageResponse()
            {
                LocationsList = locationsIdList,
                PageNumber = parameters.CurrentPageNumber,
                TotalItems = locationsList.Count,
                TagsList = tagsList
            });
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