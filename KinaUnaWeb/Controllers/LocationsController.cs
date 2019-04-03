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
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public LocationsController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<string> tagsList = new List<string>();

            // ToDo: Implement _progenyHttpClient.GetLocations() 
            var locationsList = await _progenyHttpClient.GetLocationsList(_progId, userAccessLevel); // _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == _progId).OrderBy(l => l.Date).ToList();
            if (!string.IsNullOrEmpty(tagFilter))
            {
                locationsList = locationsList.Where(l => l.Tags.Contains(tagFilter)).ToList();
            }
            locationsList = locationsList.OrderBy(l => l.Date).ToList();

            LocationViewModel model = new LocationViewModel();

            model.IsAdmin = _userIsProgenyAdmin;
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
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            LocationViewModel model = new LocationViewModel();
            model.LocationsList = new List<Location>();
            List<string> tagsList = new List<string>();
            model.ProgenyId = _progId;
            model.Progeny = progeny;
            List<Picture> pictures = await _mediaHttpClient.GetPictureList(progeny.Id, userAccessLevel, userinfo.Timezone);
            if (String.IsNullOrEmpty(tagFilter))
            {
                pictures = pictures.FindAll(p => !string.IsNullOrEmpty(p.Longtitude));
            }
            else
            {
                pictures = pictures.FindAll(p =>
                    !string.IsNullOrEmpty(p.Longtitude) && p.Tags.ToUpper().Contains(tagFilter.ToUpper()));
            }
            pictures = pictures.OrderBy(p => p.PictureTime).ToList();
            List<Picture> locPictures = new List<Picture>();
            foreach (Picture pic in pictures)
            {
                Location picLoc = new Location();
                bool validCoords = true;
                double lat;
                if (double.TryParse(pic.Latitude, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out lat))
                {
                    picLoc.Latitude = lat;
                }
                else
                {
                    validCoords = false;
                }

                double lon;
                if (double.TryParse(pic.Longtitude, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out lon))
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
                    if (!String.IsNullOrEmpty(locPic.Tags))
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