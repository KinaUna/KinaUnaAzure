using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for CalendarItems.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="calendarService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ICalendarService calendarService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Progenies([FromBody] CalendarItemsRequest request)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<Progeny> progenyList = [];
            foreach (int progenyId in request.ProgenyIds)
            {
                Progeny progeny = await progenyService.GetProgeny(progenyId);
                if (progeny != null)
                {
                    UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                    if (userAccess != null)
                    {
                        progenyList.Add(progeny);
                    }
                }
            }

            List<CalendarItem> calendarList = [];

            if (progenyList.Count == 0) return NotFound();
            foreach (Progeny progeny in progenyList)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progeny.Id, userEmail);
                List<CalendarItem> progenyCalendarItems = await calendarService.GetCalendarList(progeny.Id, userAccess.AccessLevel);
                calendarList.AddRange(progenyCalendarItems);
            }

            return Ok(calendarList);
        }
        
        /// <summary>
        /// Retrieves a single CalendarItem with a given id.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to retrieve</param>
        /// <returns>CalendarItem with the id provided. Start and end times are in UTC timezone.</returns>
        // GET api/calendar/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCalendarItem(int id)
        {
            CalendarItem result = await calendarService.GetCalendarItem(id);
            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(result.ProgenyId, userEmail, result.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Adds a new CalendarItem to the database.
        /// Then adds a TimeLineItem for the new CalendarItem.
        /// Then sends a notification to all users with access to the CalendarItem.
        /// </summary>
        /// <param name="value">CalendarItem to add. The start and end time should be in the UTC timezone.</param>
        /// <returns>The added CalendarItem. Start and end times are in UTC timezone.</returns>
        // POST api/calendar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CalendarItem value)
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
                return BadRequest();
            }

            value.Author = User.GetUserId();

            CalendarItem calendarItem = await calendarService.AddCalendarItem(value);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            TimeLineItem timeLineItem = calendarItem.ToNewTimeLineItem();
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            await NotifyCalendarItemAdded(progeny, userInfo, timeLineItem, calendarItem);

            return Ok(calendarItem);
        }

        private async Task NotifyCalendarItemAdded(Progeny progeny, UserInfo userInfo, TimeLineItem timeLineItem, CalendarItem calendarItem )
        {
            string notificationTitle = "Calendar item added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new calendar item for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);

            await webNotificationsService.SendCalendarNotification(calendarItem, userInfo, notificationTitle);
        }

        /// <summary>
        /// Updates an existing CalendarItem in the database.
        /// Then updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem</param>
        /// <param name="value">The CalendarItem with the properties to update.The start and end times should be in UTC timezone.</param>
        /// <returns>The updated CalendarItem. Start and end times are in UTC timezone.</returns>
        // PUT api/calendar/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] CalendarItem value)
        {
            CalendarItem calendarItem = await calendarService.GetCalendarItem(id);

            if (calendarItem == null)
            {
                return NotFound();
            }

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
                return BadRequest();
            }

            calendarItem = await calendarService.UpdateCalendarItem(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);

            if (timeLineItem == null || !timeLineItem.CopyCalendarItemPropertiesForUpdate(calendarItem)) return Ok(calendarItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem);
            
            return Ok(calendarItem);
        }

        /// <summary>
        /// Deletes a CalendarItem from the database.
        /// Then deletes the corresponding TimeLineItem.
        /// Notifies all users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to delete</param>
        /// <returns>NoContentResult if successful, UnauthorizedResult if the user does not have access, NotFoundResult if the CalendarItem was not found.</returns>
        // DELETE api/calendar/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            CalendarItem calendarItem = await calendarService.GetCalendarItem(id);
            if (calendarItem == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Progeny progeny = await progenyService.GetProgeny(calendarItem.ProgenyId);
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            await calendarService.DeleteCalendarItem(calendarItem);

            if (timeLineItem == null) return NoContent();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Calendar item deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a calendar item for " + progeny.NickName + ". Event: " + calendarItem.Title;

            calendarItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendCalendarNotification(calendarItem, userInfo, notificationTitle);

            return NoContent();

        }
    }
}
