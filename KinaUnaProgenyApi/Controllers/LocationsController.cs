using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// <summary>
        /// Retrieves all locations for a given progeny that a user with a given access level has access to.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Location items for.</param>
        /// <param name="accessLevel">The user's access level for this Progeny.</param>
        /// <returns>List of Locations, or NotFound if no Locations are found.</returns>
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
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(location.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(location);
            }

            return Unauthorized();
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
        /// Retrieves a Location entity for a mobile client.
        /// </summary>
        /// <param name="id">The LocationId for the Location entity to get.</param>
        /// <returns>The Location object with the provided LocationId.</returns>
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

        /// <summary>
        /// Retrieves the list of Location items to display on a page for a given Progeny.
        /// </summary>
        /// <param name="pageSize">The number of Location items per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to display Locations for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="sortBy">int: Sort order for the Location items. 0 = oldest first, 1 = newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Mobile clients still use this parameter.")]
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
