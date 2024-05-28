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
        // GET api/measurements/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Measurement> measurementsList = await measurementService.GetMeasurementsList(id);
            measurementsList = measurementsList.Where(m => m.AccessLevel >= accessLevel).ToList();
            if (measurementsList.Count != 0)
            {
                return Ok(measurementsList);
            }
            return NotFound();

        }

        // GET api/measurements/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMeasurementItem(int id)
        {
            Measurement result = await measurementService.GetMeasurement(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

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

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Measurement edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a measurement for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);

            return Ok(measurementItem);
        }

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

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await measurementService.GetMeasurement(id);

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
        public async Task<IActionResult> GetMeasurementsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
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

            List<Measurement> allItems = await measurementService.GetMeasurementsList(progenyId);
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
