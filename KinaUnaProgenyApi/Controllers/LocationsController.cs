using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ILocationService _locationService;
        private readonly ITimelineService _timelineService;
        private readonly IProgenyService _progenyService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public LocationsController(IAzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService, ILocationService locationService,
            ITimelineService timelineService, IProgenyService progenyService, IWebNotificationsService webNotificationsService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _locationService = locationService;
            _timelineService = timelineService;
            _progenyService = progenyService;
            _webNotificationsService = webNotificationsService;
        }

        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Location> locationsList = await _locationService.GetLocationsList(id);
                locationsList = locationsList.Where(l => l.AccessLevel >= accessLevel).ToList();
                if (locationsList.Any())
                {
                    return Ok(locationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/locations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location
                result = await _locationService.GetLocation(id);

            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Location value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            value.Author = User.GetUserId();

            Location location = await _locationService.AddLocation(value);
            
            TimeLineItem tItem = new TimeLineItem();
            tItem.CopyLocationPropertiesForAdd(location);
            _ = await _timelineService.AddTimeLineItem(tItem);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Location added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new location for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, tItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);

            return Ok(location);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Location value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Location location = await _locationService.GetLocation(id);
            if (location == null)
            {
                return NotFound();
            }
            
            location = await _locationService.UpdateLocation(value);
            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location);
            if (timeLineItem != null)
            {
                timeLineItem.CopyLocationPropertiesForUpdate(location);
                await _timelineService.UpdateTimeLineItem(timeLineItem);
            }

            location.Author = User.GetUserId();
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            string notificationTitle = "Location edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a location for " + progeny.NickName;
            
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);

            return Ok(location);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Location location = await _locationService.GetLocation(id);
            if (location != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(location.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (progeny != null)
                {
                    if (!progeny.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _locationService.DeleteLocation(location);
                
                if (timeLineItem != null)
                {
                    location.Author = User.GetUserId();
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                    string notificationTitle = "Location deleted for " + progeny.NickName;
                    string notificationMessage = userInfo.FullName() + " deleted a location for " + progeny.NickName + ". Location: " + location.Name;
                    location.AccessLevel = timeLineItem.AccessLevel = 0;
                    
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);
                }

                return NoContent();
            }

            return NotFound();
        }


        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _locationService.GetLocation(id); 
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await _locationService.GetLocationsList(progenyId);
            allItems = allItems.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int locationCounter = 1;
            int locationsCount = allItems.Count;
            foreach (Location location in allItems)
            {
                if (sortBy == 1)
                {
                    location.LocationNumber = locationsCount - locationCounter + 1;
                }
                else
                {
                    location.LocationNumber = locationCounter;
                }

                locationCounter++;
            }

            List<Location> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            LocationsListPage model = new LocationsListPage();
            model.LocationsList = itemsOnPage;
            model.TotalPages = (int) Math.Ceiling(allItems.Count / (double) pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }
    }
}
