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
    public class MeasurementsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly IMeasurementService _measurementService;
        private readonly IProgenyService _progenyService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public MeasurementsController(IAzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            ITimelineService timelineService, IMeasurementService measurementService, IProgenyService progenyService, IWebNotificationsService webNotificationsService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _measurementService = measurementService;
            _progenyService = progenyService;
            _webNotificationsService = webNotificationsService;
        }

        // GET api/measurements/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Measurement> measurementsList = await _measurementService.GetMeasurementsList(id);
                measurementsList = measurementsList.Where(m => m.AccessLevel >= accessLevel).ToList();
                if (measurementsList.Any())
                {
                    return Ok(measurementsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/measurements/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMeasurementItem(int id)
        {
            Measurement result = await _measurementService.GetMeasurement(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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

            Measurement measurementItem = await _measurementService.AddMeasurement(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyMeasurementPropertiesForAdd(measurementItem);

            
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            await _timelineService.AddTimeLineItem(timeLineItem);

            string notificationTitle = "Measurement added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new measurement for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);

            return Ok(measurementItem);
        }

        // PUT api/measurement/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Measurement value)
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

            Measurement measurementItem = await _measurementService.GetMeasurement(id);
            if (measurementItem == null)
            {
                return NotFound();
            }
            
            measurementItem = await _measurementService.UpdateMeasurement(value);
            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(measurementItem.MeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement);
            if (timeLineItem != null)
            {
                timeLineItem.CopyMeasurementPropertiesForUpdate(measurementItem);
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);

                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Measurement edited for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " edited a measurement for " + progeny.NickName;
                
                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);
            }
            
            return Ok(measurementItem);
        }

        // DELETE api/measurements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Measurement measurementItem = await _measurementService.GetMeasurement(id);
            if (measurementItem != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(measurementItem.ProgenyId);
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

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(measurementItem.MeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _measurementService.DeleteMeasurement(measurementItem);
                
                if (timeLineItem != null)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                    string notificationTitle = "Measurement deleted for " + progeny.NickName;
                    string notificationMessage = userInfo.FullName() + " deleted a measurement for " + progeny.NickName + ". Measurement date: " + measurementItem.Date.Date.ToString("dd-MMM-yyyy");

                    measurementItem.AccessLevel = timeLineItem.AccessLevel = 0;

                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendMeasurementNotification(measurementItem, userInfo, notificationTitle);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await _measurementService.GetMeasurement(id);

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
        public async Task<IActionResult> GetMeasurementsListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
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

            List<Measurement> allItems = await _measurementService.GetMeasurementsList(progenyId);
            allItems = allItems.OrderBy(m => m.Date).ToList();

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
