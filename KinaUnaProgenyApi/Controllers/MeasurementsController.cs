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
    /// API endpoints for Measurements.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="measurementService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class MeasurementsController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ITimelineService timelineService,
        IMeasurementService measurementService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves all Measurements for a given Progeny that a user with the given access level can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Measurement items for.</param>
        /// <returns>List of Measurements.</returns>
        // GET api/measurements/progeny/[id]
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

            List<Measurement> measurementsList = await measurementService.GetMeasurementsList(id, accessLevelResult.Value);
            
            return Ok(measurementsList);
        }

        /// <summary>
        /// Retrieves a specific Measurement by id.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to get.</param>
        /// <returns>The Measurement object with the provided MeasurementId</returns>
        // GET api/measurements/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMeasurementItem(int id)
        {
            Measurement measurement = await measurementService.GetMeasurement(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(measurement.ProgenyId, userEmail, measurement.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(measurement);
        }

        /// <summary>
        /// Adds a new Measurement entity to the database.
        /// Then adds a TimeLineItem and sends notifications to users who have access to the Measurement.
        /// </summary>
        /// <param name="value">The Measurement object to add.</param>
        /// <returns>The added Measurement object. UnauthorizedResult if the user doesn't have the access right to add items. NotFoundResult if the Progeny doesn't exist.</returns>
        // POST api/measurements
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Measurement value)
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

            Measurement measurementItem = await measurementService.AddMeasurement(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyMeasurementPropertiesForAdd(measurementItem);


            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            await timelineService.AddTimeLineItem(timeLineItem);

            string notificationTitle = "Measurement added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new measurement for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);

            return Ok(measurementItem);
        }

        /// <summary>
        /// Updates an existing Measurement entity in the database.
        /// Then updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to update.</param>
        /// <param name="value">Measurement object with the properties to update.</param>
        /// <returns>The updated Measurement object.</returns>
        // PUT api/measurement/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Measurement value)
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

            Measurement measurementItem = await measurementService.GetMeasurement(id);
            if (measurementItem == null)
            {
                return NotFound();
            }

            measurementItem = await measurementService.UpdateMeasurement(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(measurementItem.MeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement);
            if (timeLineItem == null) return Ok(measurementItem);

            timeLineItem.CopyMeasurementPropertiesForUpdate(measurementItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            return Ok(measurementItem);
        }

        /// <summary>
        /// Deletes a Measurement entity from the database.
        /// Then deletes the corresponding TimeLineItem and sends notifications to users who have admin access to the Progeny.
        /// </summary>
        /// <param name="id">The MeasurementId of the Measurement entity to delete.</param>
        /// <returns>NoContentResult if the deletion was successful. NotFoundResult if the Measurement doesn't exist. UnauthorizedResult if the user doesn't have admin access for the Progeny.</returns>
        // DELETE api/measurements/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Measurement measurementItem = await measurementService.GetMeasurement(id);
            if (measurementItem != null)
            {
                Progeny progeny = await progenyService.GetProgeny(measurementItem.ProgenyId);
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

                TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(measurementItem.MeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement);
                if (timeLineItem != null)
                {
                    _ = await timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await measurementService.DeleteMeasurement(measurementItem);

                if (timeLineItem == null) return NoContent();

                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Measurement deleted for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " deleted a measurement for " + progeny.NickName + ". Measurement date: " + measurementItem.Date.Date.ToString("dd-MMM-yyyy");

                measurementItem.AccessLevel = timeLineItem.AccessLevel = 0;

                await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);

                return NoContent();
            }

            return NotFound();
        }
        
        /// <summary>
        /// Retrieves the list of Measurement items to display on a page for a given Progeny.
        /// </summary>
        /// <param name="pageSize">The number of Measurement items per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Measurements for.</param>
        /// <param name="sortBy">int: Sort order for the Measurement items. 0 = oldest first, 1 = newest first.</param>
        /// <returns>List of Measurement items.</returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetMeasurementsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int sortBy = 1)
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

            List<Measurement> allItems = await measurementService.GetMeasurementsList(progenyId, accessLevelResult.Value);
            allItems = [.. allItems.OrderBy(m => m.Date)];

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int measurementsCounter = 1;
            int measurementsCount = allItems.Count;
            foreach (Measurement mes in allItems)
            {
                if (sortBy == 1)
                {
                    mes.MeasurementNumber = measurementsCount - measurementsCounter + 1;
                }
                else
                {
                    mes.MeasurementNumber = measurementsCounter;
                }

                measurementsCounter++;
            }

            List<Measurement> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            MeasurementsListPage model = new()
            {
                MeasurementsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }
    }
}
