﻿using KinaUnaWeb.Models.ItemViewModels;
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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;

        public LocationsController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, IUserInfosHttpClient userInfosHttpClient,
            ILocationsHttpClient locationsHttpClient, IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
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

        public async Task<IActionResult> AddLocation()
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    model.Progeny = accessList[0];
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                            model.Progeny = prog;
                        }
                        model.ProgenyList.Add(selItem);

                        List<Location> locList1 = await _locationsHttpClient.GetLocationsList(prog.Id, 0);
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

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;
            model.Latitude = 30.94625288456589;
            model.Longitude = -54.10861860580418;
            model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.Progeny = prog;
            Location locItem = new Location();
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
            }
            if (!string.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            locItem.ProgenyId = model.ProgenyId;
            locItem.DateAdded = DateTime.UtcNow;
            locItem.Author = model.CurrentUser.UserId;
            locItem.AccessLevel = model.AccessLevel;

            await _locationsHttpClient.AddLocation(locItem);

            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= locItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime tempDate = DateTime.UtcNow;
                        if (locItem.Date.HasValue)
                        {
                            tempDate = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
                        }

                        string dateString = tempDate.ToString("dd-MMM-yyyy");
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + locItem.Name + "\r\nDate: " + dateString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new location was added for " + progeny.NickName;
                        notification.Link = "/Locations?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunalocation" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> EditLocation(int itemId)
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny chld in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == chld.Id).NickName,
                            Value = chld.Id.ToString()
                        };
                        if (chld.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);

                        List<Location> locList1 = await _locationsHttpClient.GetLocationsList(chld.Id, 0);
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

            Location locItem = await _locationsHttpClient.GetLocation(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            model.Progeny = prog;
            model.LocationId = locItem.LocationId;
            model.Latitude = locItem.Latitude;
            model.Longitude = locItem.Longitude;
            model.Name = locItem.Name;
            model.HouseNumber = locItem.HouseNumber;
            model.StreetName = locItem.StreetName;
            model.District = locItem.District;
            model.City = locItem.City;
            model.PostalCode = locItem.PostalCode;
            model.County = locItem.County;
            model.State = locItem.State;
            model.Country = locItem.Country;
            model.Notes = locItem.Notes;
            if (locItem.Date.HasValue)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.Tags = locItem.Tags;
            model.ProgenyId = locItem.ProgenyId;
            model.DateAdded = locItem.DateAdded;
            model.Author = locItem.Author;
            model.AccessLevel = locItem.AccessLevel;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Location locItem = await _locationsHttpClient.GetLocation(model.LocationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.Progeny = prog;
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (!string.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            locItem.AccessLevel = model.AccessLevel;

            await _locationsHttpClient.UpdateLocation(locItem);

            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> DeleteLocation(int itemId)
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Location = await _locationsHttpClient.GetLocation(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Location.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
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
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Location locItem = await _locationsHttpClient.GetLocation(model.Location.LocationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _locationsHttpClient.DeleteLocation(locItem.LocationId);
            return RedirectToAction("Index", "Locations");
        }
    }
}