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
    public class LocationsController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ILocationService locationService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Location> locationsList = await locationService.GetLocationsList(id);
            locationsList = locationsList.Where(l => l.AccessLevel >= accessLevel).ToList();
            if (locationsList.Count != 0)
            {
                return Ok(locationsList);
            }

            return NotFound();

        }

        // GET api/locations/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location
                result = await locationService.GetLocation(id);

            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            Location location = await locationService.AddLocation(value);

            TimeLineItem tItem = new();
            tItem.CopyLocationPropertiesForAdd(location);
            _ = await timelineService.AddTimeLineItem(tItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Location added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new location for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, tItem, userInfo.ProfilePicture);
            await webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);

            return Ok(location);
        }

        // PUT api/timeline/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Location value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            Location location = await locationService.GetLocation(id);
            if (location == null)
            {
                return NotFound();
            }

            location = await locationService.UpdateLocation(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location);
            if (timeLineItem != null)
            {
                timeLineItem.CopyLocationPropertiesForUpdate(location);
                await timelineService.UpdateTimeLineItem(timeLineItem);
            }

            location.Author = User.GetUserId();
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Location edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a location for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);

            return Ok(location);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Location location = await locationService.GetLocation(id);
            if (location == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(location.ProgenyId);
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

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await locationService.DeleteLocation(location);

            if (timeLineItem == null) return NoContent();

            location.Author = User.GetUserId();
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Location deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a location for " + progeny.NickName + ". Location: " + location.Name;
            location.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendLocationNotification(location, userInfo, notificationTitle);

            return NoContent();

        }


        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await locationService.GetLocation(id);
            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();

        }

        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await locationService.GetLocationsList(progenyId);
            allItems = [.. allItems.OrderBy(v => v.Date)];

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

            LocationsListPage model = new()
            {
                LocationsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }
    }
}
