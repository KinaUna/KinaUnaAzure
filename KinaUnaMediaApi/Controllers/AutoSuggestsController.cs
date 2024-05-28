﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaMediaApi.Services;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AutoSuggestsController(IDataService dataService, IPicturesService picturesService, IVideosService videosService) : ControllerBase
    {
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }


            List<Picture> allItems = await picturesService.GetPicturesList(id);
            allItems = allItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allItems)
            {
                if (!string.IsNullOrEmpty(picture.Location))
                {
                    if (!autoSuggestList.Contains(picture.Location))
                    {
                        autoSuggestList.Add(picture.Location);
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Location)) continue;

                if (!autoSuggestList.Contains(video.Location))
                {
                    autoSuggestList.Add(video.Location);
                }
            }

            List<CalendarItem> allCalendarItems = await dataService.GetCalendarList(id);
            allCalendarItems = allCalendarItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (string.IsNullOrEmpty(calendarItem.Location)) continue;

                if (!autoSuggestList.Contains(calendarItem.Location))
                {
                    autoSuggestList.Add(calendarItem.Location);
                }
            }

            List<Location> allLocations = await dataService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location locationItem in allLocations)
            {
                if (string.IsNullOrEmpty(locationItem.Name)) continue;

                if (!autoSuggestList.Contains(locationItem.Name))
                {
                    autoSuggestList.Add(locationItem.Name);
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }

        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }


            List<Picture> allItems = await picturesService.GetPicturesList(id);
            allItems = allItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allItems)
            {
                if (string.IsNullOrEmpty(picture.Tags)) continue;

                List<string> tagsList = [.. picture.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Tags)) continue;

                List<string> tagsList = [.. video.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Location> allLocations = await dataService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location location in allLocations)
            {
                if (string.IsNullOrEmpty(location.Tags)) continue;

                List<string> tagsList = [.. location.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Friend> allFriends = await dataService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friend in allFriends)
            {
                if (string.IsNullOrEmpty(friend.Tags)) continue;

                List<string> tagsList = [.. friend.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Contact> allContacts = await dataService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contact in allContacts)
            {
                if (string.IsNullOrEmpty(contact.Tags)) continue;

                List<string> tagsList = [.. contact.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }
    }
}
