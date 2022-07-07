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
                calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel && c.EndTime > startDate && c.StartTime < endDate ).ToList();
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
                if (userAccess != null || id == Constants.DefaultChildId)
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
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add calendar items for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            CalendarItem calendarItem = new CalendarItem();
            calendarItem.AccessLevel = value.AccessLevel;
            calendarItem.Author = value.Author;
            calendarItem.Notes = value.Notes;
            calendarItem.ProgenyId = value.ProgenyId;
            calendarItem.AllDay = value.AllDay;
            calendarItem.Context = value.Context;
            calendarItem.Location = value.Location;
            calendarItem.Title = value.Title;
            calendarItem.StartTime = value.StartTime;
            calendarItem.EndTime = value.EndTime;

            calendarItem = await _calendarService.AddCalendarItem(calendarItem);
            
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = calendarItem.ProgenyId;
            tItem.AccessLevel = calendarItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Calendar;
            tItem.ItemId = calendarItem.EventId.ToString();
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            tItem.CreatedBy = userinfo?.UserId ?? "User not found";
            tItem.CreatedTime = DateTime.UtcNow;
            if (calendarItem.StartTime != null)
            {
                tItem.ProgenyTime = calendarItem.StartTime.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            _ = await _timelineService.AddTimeLineItem(tItem);
            
            string title = "Calendar item added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new calendar item for " + prog.NickName;

                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }
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

            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit calendar items for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            calendarItem.AccessLevel = value.AccessLevel;
            calendarItem.Author = value.Author;
            calendarItem.Notes = value.Notes;
            calendarItem.ProgenyId = value.ProgenyId;
            calendarItem.AllDay = value.AllDay;
            calendarItem.Context = value.Context;
            calendarItem.Location = value.Location;
            calendarItem.Title = value.Title;
            calendarItem.StartTime = value.StartTime;
            calendarItem.EndTime = value.EndTime;

            calendarItem = await _calendarService.UpdateCalendarItem(calendarItem);

            TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);

            if (tItem != null && calendarItem.StartTime.HasValue && calendarItem.EndTime.HasValue)
            {
                tItem.ProgenyTime = calendarItem.StartTime.Value;
                tItem.AccessLevel = calendarItem.AccessLevel;
                _ = await _timelineService.UpdateTimeLineItem(tItem);
               
            }
            
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Calendar edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a calendar item for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

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
                // Check if child exists.
                Progeny prog = await _progenyService.GetProgeny(calendarItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to edit calendar items for this child.
                    
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(calendarItem.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);
                if (tItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(tItem);
                    
                }

                await _calendarService.DeleteCalendarItem(calendarItem);
                
                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string title = "Calendar item deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a calendar item for " + prog.NickName + ". Event: " + calendarItem.Title;

                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
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
                List<CalendarItem> model = await _calendarService.GetCalendarList(progenyId);
                model = model.Where(c => c.EndTime > DateTime.UtcNow && c.AccessLevel >= accessLevel).ToList();
                model = model.OrderBy(e => e.StartTime).ToList();
                model = model.Take(5).ToList();

                return Ok(model);
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            CalendarItem result = await _calendarService.GetCalendarItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null)
            {
                return Ok(result);
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
                calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();
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
