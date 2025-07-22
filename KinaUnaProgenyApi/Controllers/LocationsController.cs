using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Locations.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="locationService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
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
        /// <summary>
        /// Retrieves all locations for a given progeny that a user with a given access level has access to.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Location items for.</param>
        /// <returns>List of Locations, or NotFound if no Locations are found.</returns>
        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Location> locationsList = await locationService.GetLocationsList(id, accessLevelResult.Value);
            
            return Ok(locationsList);
        }

        /// <summary>
        /// Retrieves the Location with a given id.
        /// </summary>
        /// <param name="id">The LocationId of the Location entity to get.</param>
        /// <returns>Location object with provided id, NotFoundResult if it doesn't exist. UnauthorizedResult if the user doesn't have the required access level.</returns>
        // GET api/locations/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location location = await locationService.GetLocation(id);

            if (location == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(location.ProgenyId, userEmail, location.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }
            
            return Ok(location);
        }

        /// <summary>
        /// Adds a new Location entity to the database.
        /// Then adds a TimeLineItem for the Location.
        /// Then sends notifications to users who have access to the Location item.
        /// </summary>
        /// <param name="value">The Location object to add.</param>
        /// <returns>The added Location object if successful. NotAuthorizedResult if the user doesn't have access right to add items. NotFoundResult if the Progeny doesn't exist.</returns>
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

        /// <summary>
        /// Updates an existing Location entity in the database.
        /// Then updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The LocationId of the Location entity to update.</param>
        /// <param name="value">Location object with the properties to update.</param>
        /// <returns>The updated Location object.</returns>
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
            
            return Ok(location);
        }

        /// <summary>
        /// Deletes a Location entity from the database.
        /// Then deletes the corresponding TimeLineItem.
        /// Then sends notifications to the Progeny's admin users.
        /// </summary>
        /// <param name="id">The LocationId of the Location entity to delete.</param>
        /// <returns>NoContentResult if successful. UnauthorizedResult if the user is denied access. NotFoundResult if the Location item doesn't exist.</returns>
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
        
        /// <summary>
        /// Retrieves the list of Location items to display on a page for a given Progeny.
        /// </summary>
        /// <param name="pageSize">The number of Location items per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to display Locations for.</param>
        /// <param name="sortBy">int: Sort order for the Location items. 0 = oldest first, 1 = newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }
            
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await locationService.GetLocationsList(progenyId, accessLevelResult.Value);
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
