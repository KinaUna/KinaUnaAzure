using System;
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
    public class CalendarController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ICalendarService _calendarService;
        private readonly ITimelineService _timelineService;
        private readonly IProgenyService _progenyService;
        private readonly AzureNotifications _azureNotifications;

        public CalendarController(AzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            ICalendarService calendarService, ITimelineService timelineService, IProgenyService progenyService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _calendarService = calendarService;
            _timelineService = timelineService;
            _progenyService = progenyService;
        }

        // GET api/calendar/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<CalendarItem> calendarList = await _calendarService.GetCalendarList(id);
                calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();
                if (calendarList.Any())
                {
                    return Ok(calendarList);
                }
            }

            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ProgenyInterval(int id, [FromQuery] string start, [FromQuery] string end, [FromQuery] int accessLevel = 5)
        {
            bool startParsed = DateTime.TryParseExact(start, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime startDate);
            bool endParsed = DateTime.TryParseExact(end, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime endDate);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId && startParsed && endParsed)
            {
                List<CalendarItem> calendarList = await _calendarService.GetCalendarList(id);
                calendarList = calendarList.Where(c => userAccess != null && c.AccessLevel >= userAccess.AccessLevel && c.EndTime > startDate && c.StartTime < endDate ).ToList();
                if (calendarList.Any())
                {
                    return Ok(calendarList);
                }
            }

            return NotFound();
        }

        // GET api/calendar/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCalendarItem(int id)
        {
            CalendarItem result = await _calendarService.GetCalendarItem(id);
            if(result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
                if ((userAccess != null && userAccess.AccessLevel <= result.AccessLevel) || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }
            }            

            return NotFound();
        }

        // POST api/calendar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CalendarItem value)
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

            CalendarItem calendarItem = await _calendarService.AddCalendarItem(value);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            TimeLineItem timeLineItem = new TimeLineItem();
            
            timeLineItem.CopyCalendarItemPropertiesForAdd(calendarItem, userInfo.UserId);
            
            _ = await _timelineService.AddTimeLineItem(timeLineItem);
            
            string notificationTitle = "Calendar item added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new calendar item for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            
            return Ok(calendarItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] CalendarItem value)
        {
            CalendarItem calendarItem = await _calendarService.GetCalendarItem(id);

            if (calendarItem == null)
            {
                return NotFound();
            }
            
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
            
            calendarItem = await _calendarService.UpdateCalendarItem(value);

            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);

            if (timeLineItem != null && timeLineItem.CopyCalendarItemPropertiesForUpdate(calendarItem))
            {
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);
            }
            
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Calendar edited for " + progeny.NickName;
            string message = userinfo.FullName() + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a calendar item for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, timeLineItem, userinfo.ProfilePicture);

            return Ok(calendarItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            CalendarItem calendarItem = await _calendarService.GetCalendarItem(id);
            if (calendarItem != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                
                Progeny progeny = await _progenyService.GetProgeny(calendarItem.ProgenyId);
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

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                await _calendarService.DeleteCalendarItem(calendarItem);
                
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string title = "Calendar item deleted for " + progeny.NickName;
                string message = userInfo.FullName() + " deleted a calendar item for " + progeny.NickName + ". Event: " + calendarItem.Title;

                if (timeLineItem != null)
                {
                    timeLineItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, timeLineItem, userInfo.ProfilePicture);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess != null || progenyId == Constants.DefaultChildId)
            {
                List<CalendarItem> calendarList = await _calendarService.GetCalendarList(progenyId);
                calendarList = calendarList.Where(c => userAccess != null && c.EndTime > DateTime.UtcNow && c.AccessLevel >= userAccess.AccessLevel).ToList();
                calendarList = calendarList.OrderBy(e => e.StartTime).ToList();
                calendarList = calendarList.Take(8).ToList();

                return Ok(calendarList);
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            CalendarItem calendarItem = await _calendarService.GetCalendarItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(calendarItem.ProgenyId, userEmail);
            if (userAccess != null && userAccess.AccessLevel <= calendarItem.AccessLevel)
            {
                return Ok(calendarItem);
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null)
            {
                List<CalendarItem> calendarList = await _calendarService.GetCalendarList(id);
                calendarList = calendarList.Where(c => c.AccessLevel >= userAccess.AccessLevel).ToList();
                if (calendarList.Any())
                {
                    return Ok(calendarList);
                }

                return NotFound();
            }

            return Unauthorized();
        }
    }
}
