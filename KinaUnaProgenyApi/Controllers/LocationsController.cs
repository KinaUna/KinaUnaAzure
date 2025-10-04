using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Locations.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
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
        ILocationService locationService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IFamiliesService familiesService,
        IWebNotificationsService webNotificationsService,
        IAccessManagementService accessManagementService)
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<Location> locationsList = await locationService.GetLocationsList(id, 0, currentUserInfo);
            
            return Ok(locationsList);
        }

        /// <summary>
        /// Retrieves all locations for a given family that a user with a given access level has access to.
        /// </summary>
        /// <param name="id">The FamilyId of the Family to get Location items for.</param>
        /// <returns>List of Locations, or NotFound if no Locations are found.</returns>
        // GET api/locations/family/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Family(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<Location> locationsList = await locationService.GetLocationsList(0, id, currentUserInfo);

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Location location = await locationService.GetLocation(id, currentUserInfo);

            if (location == null)
            {
                return NotFound();
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

            // Either ProgenyId or FamilyId must be set, but not both.
            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A location must have either a ProgenyId or a FamilyId set, but not both.");
            }

            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A location board must have either a ProgenyId or a FamilyId set.");
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (value.ProgenyId > 0)
            {
                if (!await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    return Unauthorized();
                }
            }

            if (value.FamilyId > 0)
            {
                if (!await accessManagementService.HasFamilyPermission(value.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    return Unauthorized();
                }
            }

            value.Author = User.GetUserId();
            value.CreatedBy = User.GetUserId();
            value.ModifiedBy = User.GetUserId();

            Location location = await locationService.AddLocation(value, currentUserInfo);
            if (location == null)
            {
                return Unauthorized();
            }

            TimeLineItem tItem = new();
            tItem.CopyLocationPropertiesForAdd(location);
            _ = await timelineService.AddTimeLineItem(tItem, currentUserInfo);

            string nameString = "";
            if (location.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(location.ProgenyId, currentUserInfo);
                if (progeny != null)
                {
                    nameString = progeny.NickName;
                }
            }
            if (location.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(location.FamilyId, currentUserInfo);
                if (family != null)
                {
                    nameString = family.Name;
                }
            }


            string notificationTitle = "Location added for " + nameString; // Todo: Localize.
            string notificationMessage = currentUserInfo.FullName() + " added a new location for " + nameString;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, tItem, currentUserInfo.ProfilePicture);
            await webNotificationsService.SendLocationNotification(location, currentUserInfo, notificationTitle);

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (id != value.LocationId)
            {
                return BadRequest("LocationId in the URL must match the LocationId in the body of the request.");
            }

            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A location must have either a ProgenyId or a FamilyId set, but not both.");
            }
            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A location board must have either a ProgenyId or a FamilyId set.");
            }

            
            Location location = await locationService.GetLocation(id, currentUserInfo);
            if (location == null || location.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return NotFound();
            }

            location.ModifiedBy = User.GetUserId();

            location = await locationService.UpdateLocation(value, currentUserInfo);
            if (location == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, currentUserInfo);
            if (timeLineItem != null)
            {
                timeLineItem.CopyLocationPropertiesForUpdate(location);
                await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Location location = await locationService.GetLocation(id, currentUserInfo);
            if (location == null || location.ItemPerMission.PermissionLevel < PermissionLevel.Admin) return NotFound();
            
            location.ModifiedBy = User.GetUserId();

            Location deletedLocation = await locationService.DeleteLocation(location, currentUserInfo);
            if (deletedLocation == null)
            {
                return Unauthorized();
            }
            
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }
            if (timeLineItem == null) return NoContent();

            location.Author = User.GetUserId();
            string nameString = "";
            if (location.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(location.ProgenyId, currentUserInfo);
                if (progeny != null)
                {
                    nameString = progeny.NickName;
                }
            }

            if (location.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(location.FamilyId, currentUserInfo);
                if (family != null)
                {
                    nameString = family.Name;
                }
            }
            string notificationTitle = "Location deleted for " + nameString;
            string notificationMessage = currentUserInfo.FullName() + " deleted a location for " + nameString + ". Location: " + location.Name;
            
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, currentUserInfo.ProfilePicture);
            await webNotificationsService.SendLocationNotification(location, currentUserInfo, notificationTitle);

            return NoContent();

        }

        /// <summary>
        /// Retrieves the list of Location items to display on a page for a given Progeny.
        /// </summary>
        /// <param name="pageSize">The number of Location items per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to display Locations for.</param>
        /// <param name="familyId">The FamilyId of the Family to display Locations for.</param>
        /// <param name="sortBy">int: Sort order for the Location items. 0 = oldest first, 1 = newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int familyId = 0, [FromQuery] int sortBy = 1)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await locationService.GetLocationsList(progenyId, familyId, currentUserInfo);
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

            List<Location> itemsOnPage = [.. allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)];

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
