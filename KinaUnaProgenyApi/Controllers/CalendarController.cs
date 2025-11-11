using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for CalendarItems.
    /// </summary>
    /// <param name="userInfoService"></param>
    /// <param name="calendarService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController(
        IUserInfoService userInfoService,
        ICalendarService calendarService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IFamiliesService familiesService,
        IWebNotificationsService webNotificationsService,
        IAccessManagementService accessManagementService)
        : ControllerBase
    {
        /// <summary>
        /// Gets a list of CalendarItems for a given Progeny.
        /// </summary>
        /// <param name="request">The parameters for the request, including the ProgenyId, start date, and end date.</param>
        /// <returns>
        /// Returns a 200 OK response with a list of CalendarItem objects for the specified progeny if accessible by the user.
        /// Returns a 404 Not Found response if no accessible progeny is found.
        /// </returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Progenies([FromBody] CalendarItemsRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Progeny> progenyList = [];
            foreach (int progenyId in request.ProgenyIds)
            {
                Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                if (progeny != null)
                {
                    progenyList.Add(progeny);
                }
            }

            List<Family> familyList = [];
            foreach (int familyId in request.FamilyIds)
            {
                Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                if (family != null)
                {
                    familyList.Add(family);
                }
            }

            List<CalendarItem> calendarList = [];

            if (progenyList.Count > 0)
            {
                foreach (Progeny progeny in progenyList)
                {
                    List<CalendarItem> progenyCalendarItems = await calendarService.GetCalendarList(progeny.Id, 0, currentUserInfo, request.StartDate, request.EndDate);
                    calendarList.AddRange(progenyCalendarItems);
                }
            }
            
            if (familyList.Count > 0)
            {
                foreach (Family family in familyList)
                {
                    List<CalendarItem> familyCalendarItems = await calendarService.GetCalendarList(0, family.FamilyId, currentUserInfo, request.StartDate, request.EndDate);
                    calendarList.AddRange(familyCalendarItems);
                }
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            CalendarItem result = await calendarService.GetCalendarItem(id, currentUserInfo);
            if (result == null) return NotFound();

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
            // Either ProgenyId or FamilyId must be set, but not both.
            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A calendar event must have either a ProgenyId or a FamilyId set, but not both.");
            }

            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A calendar event must have either a ProgenyId or a FamilyId set.");
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
            CalendarItem calendarItem = await calendarService.AddCalendarItem(value, currentUserInfo);
            if (calendarItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = calendarItem.ToNewTimeLineItem();
            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            await NotifyCalendarItemAdded(currentUserInfo, timeLineItem, calendarItem);
            calendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);

            return Ok(calendarItem);
        }

        /// <summary>
        /// Sends notifications when a new calendar item is added for a specified progeny.
        /// </summary>
        /// <remarks>This method sends both Azure and web notifications to inform relevant parties about
        /// the addition of a new calendar item.</remarks>
        /// <param name="userInfo">The user who added the calendar item. Cannot be null.</param>
        /// <param name="timeLineItem">The timeline item associated with the calendar event. Cannot be null.</param>
        /// <param name="calendarItem">The calendar item that was added. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous notification operation.</returns>
        private async Task NotifyCalendarItemAdded(UserInfo userInfo, TimeLineItem timeLineItem, CalendarItem calendarItem )
        {
            string nameString = "";
            if (timeLineItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(timeLineItem.ProgenyId, userInfo);
                if (progeny == null) return;
                nameString = progeny.NickName;
            }
            if (timeLineItem.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(timeLineItem.FamilyId, userInfo);
                if (family == null) return;
                nameString = family.Name;
            }
            string notificationTitle = "Calendar item added for " + nameString;
            
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            CalendarItem calendarItem = await calendarService.GetCalendarItem(id, currentUserInfo);

            if (calendarItem == null || calendarItem.EventId == 0 || calendarItem.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            value.ModifiedBy = User.GetUserId();

            calendarItem = await calendarService.UpdateCalendarItem(value, currentUserInfo);
            if (calendarItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar, currentUserInfo);
            if (timeLineItem == null)
            {
                timeLineItem = calendarItem.ToNewTimeLineItem();
                _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);
            }
            else
            {
                if (timeLineItem.CopyCalendarItemPropertiesForUpdate(calendarItem))
                {
                    _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);
                }
            }

            calendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, id, currentUserInfo, PermissionLevel.Admin))
            {
                return Unauthorized();
            }
            
            CalendarItem calendarItem = await calendarService.GetCalendarItem(id, currentUserInfo);
            if (calendarItem == null || calendarItem.EventId == 0) return NotFound();
            
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }

            calendarItem.ModifiedBy = User.GetUserId();
            
            CalendarItem deletedCalendarItem = await calendarService.DeleteCalendarItem(calendarItem, currentUserInfo);
            if (deletedCalendarItem == null)
            {
                return Unauthorized();
            }

            if (timeLineItem == null) return NoContent();
            string nameString = "";
            if (calendarItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(calendarItem.ProgenyId, currentUserInfo);
                if (progeny != null)
                {
                    nameString = progeny.NickName;
                }
            }
            if (calendarItem.FamilyId > 0)
            {
                Family family = await familiesService.GetFamilyById(calendarItem.FamilyId, currentUserInfo);
                if (family != null)
                {
                    nameString = family.Name;
                }
            }

            string notificationTitle = "Calendar item deleted for " + nameString;
            
            await webNotificationsService.SendCalendarNotification(calendarItem, currentUserInfo, notificationTitle);

            return NoContent();

        }
    }
}
