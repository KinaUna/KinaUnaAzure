﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
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
        /// <summary>
        /// Retrieves the list of all CalendarItems for a given Progeny.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny</param>
        /// <param name="accessLevel">The user's access level for this Progeny</param>
        /// <returns>List of CalendarItems</returns>
        // GET api/calendar/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return NotFound();

            List<CalendarItem> calendarList = await calendarService.GetCalendarList(id);
            calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();
            if (calendarList.Count != 0)
            {
                return Ok(calendarList);
            }

            return NotFound();
        }

        /// <summary>
        /// Retrieves the list of CalendarItems for a given Progeny within a given date interval.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny</param>
        /// <param name="start">string: The start of the interval, in the format 'dd-MM-yyy'</param>
        /// <param name="end">string: The end of the interval, in the format 'dd-MM-yyy'</param>
        /// <param name="accessLevel">The user's access level for this Progeny</param>
        /// <returns>List of CalendarItems with all the Progeny's CalendarItems within the interval.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The parameter is needed for mobile clients")]
        public async Task<IActionResult> ProgenyInterval(int id, [FromQuery] string start, [FromQuery] string end, [FromQuery] int accessLevel = 5)
        {
            bool startParsed = DateTime.TryParseExact(start, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime startDate);
            bool endParsed = DateTime.TryParseExact(end, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime endDate);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && (id != Constants.DefaultChildId || !startParsed || !endParsed)) return NotFound();

            List<CalendarItem> calendarList = await calendarService.GetCalendarList(id);
            calendarList = calendarList.Where(c => userAccess != null && c.AccessLevel >= userAccess.AccessLevel && c.EndTime > startDate && c.StartTime < endDate).ToList();
            if (calendarList.Count != 0)
            {
                return Ok(calendarList);
            }

            return NotFound();
        }

        /// <summary>
        /// Retrieves a single CalendarItem with a given id.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to retrieve</param>
        /// <returns>CalendarItem with the id provided.</returns>
        // GET api/calendar/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCalendarItem(int id)
        {
            CalendarItem result = await calendarService.GetCalendarItem(id);
            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if ((userAccess != null && userAccess.AccessLevel <= result.AccessLevel) || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return NotFound();
        }

        /// <summary>
        /// Adds a new CalendarItem to the database.
        /// Then adds a TimeLineItem for the new CalendarItem.
        /// Then sends a notification to all users with access to the CalendarItem.
        /// </summary>
        /// <param name="value">CalendarItem to add</param>
        /// <returns>The added CalendarItem.</returns>
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
                return NotFound();
            }

            value.Author = User.GetUserId();

            CalendarItem calendarItem = await calendarService.AddCalendarItem(value);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            TimeLineItem timeLineItem = new();

            timeLineItem.CopyCalendarItemPropertiesForAdd(calendarItem, userInfo.UserId);

            _ = await timelineService.AddTimeLineItem(timeLineItem);

            string notificationTitle = "Calendar item added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new calendar item for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);

            await webNotificationsService.SendCalendarNotification(calendarItem, userInfo, notificationTitle);

            return Ok(calendarItem);
        }

        /// <summary>
        /// Updates an existing CalendarItem in the database.
        /// Then updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem</param>
        /// <param name="value">The CalendarItem with the properties to update.</param>
        /// <returns>The updated CalendarItem.</returns>
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
                return NotFound();
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
                return NotFound();
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

        /// <summary>
        /// Retrieves the first upcoming CalendarItems for a given Progeny.
        /// Default number of items is set in Constants.DefaultUpcomingCalendarItemsCount.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get CalendarItems for.</param>
        /// <param name="accessLevel">The user's access level for this Progeny.</param>
        /// <returns>List of CalendarItems.</returns>
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess == null && progenyId != Constants.DefaultChildId) return NotFound();

            List<CalendarItem> calendarList = await calendarService.GetCalendarList(progenyId);
            calendarList = calendarList.Where(c => userAccess != null && c.EndTime > DateTime.UtcNow && c.AccessLevel >= userAccess.AccessLevel).ToList();
            calendarList = [.. calendarList.OrderBy(e => e.StartTime)];
            calendarList = calendarList.Take(Constants.DefaultUpcomingCalendarItemsCount).ToList();

            return Ok(calendarList);

        }

        /// <summary>
        /// Retrieves a single CalendarItem with a given id.
        /// For mobile clients.
        /// </summary>
        /// <param name="id">The EventId of the CalendarItem to get.</param>
        /// <returns>The CalendarItem with the provided EventId.</returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            CalendarItem calendarItem = await calendarService.GetCalendarItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(calendarItem.ProgenyId, userEmail);
            if (userAccess != null && userAccess.AccessLevel <= calendarItem.AccessLevel)
            {
                return Ok(calendarItem);
            }

            return Unauthorized();
        }

        /// <summary>
        /// Retrieves the list of all CalendarItems for a given Progeny that the user has access to.
        /// For mobile clients.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get CalendarItems for.</param>
        /// <param name="accessLevel">The user's access level for this Progeny.</param>
        /// <returns>List of CalendarItem.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null) return Unauthorized();
            List<CalendarItem> calendarList = await calendarService.GetCalendarList(id);
            calendarList = calendarList.Where(c => c.AccessLevel >= userAccess.AccessLevel).ToList();
            if (calendarList.Count != 0)
            {
                return Ok(calendarList);
            }

            return NotFound();

        }
    }
}
