using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        
        public LocationsController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, IUserInfosHttpClient userInfosHttpClient, ILocationsHttpClient locationsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);
            int userAccessLevel = (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<string> tagsList = new List<string>();

            // ToDo: Implement _progenyHttpClient.GetLocations() 
            List<Location> locationsList = await _locationsHttpClient.GetLocationsList(childId, userAccessLevel);
            if (!string.IsNullOrEmpty(tagFilter))
            {
                locationsList = locationsList.Where(l => l.Tags != null && l.Tags.Contains(tagFilter)).ToList();
            }
            locationsList = locationsList.OrderBy(l => l.Date).ToList();
            
            model.Progeny = progeny;

            if (locationsList.Any())
            {
                model.LocationsList = new List<Location>();
                foreach (Location loc in locationsList)
                {
                    if (loc.AccessLevel == (int)AccessLevel.Public || loc.AccessLevel >= userAccessLevel)
                    {
                        model.LocationsList.Add(loc);
                        if (!String.IsNullOrEmpty(loc.Tags))
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

            string tags = "";
            foreach (string tstr in tagsList)
            {
                tags = tags + tstr + ",";
            }
            model.Tags = tags.TrimEnd(',');
            
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
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);
            int userAccessLevel = (int)AccessLevel.Public;
            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            model.LocationsList = new List<Location>();
            List<string> tagsList = new List<string>();
            model.ProgenyId = childId;
            model.Progeny = progeny;
            List<Picture> pictures = await _mediaHttpClient.GetPictureList(progeny.Id, userAccessLevel, model.CurrentUser.Timezone);
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
            List<Picture> locPictures = new List<Picture>();
            foreach (Picture pic in pictures)
            {
                Location picLoc = new Location();
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

                if (validCoords && (pic.AccessLevel == (int)AccessLevel.Public || pic.AccessLevel >= userAccessLevel))
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

            string tags = "";
            foreach (string tstr in tagsList)
            {
                tags = tags + tstr + ",";
            }
            model.Tags = tags.TrimEnd(',');
            model.TagFilter = tagFilter;
            return View(model);
        }
    }
}