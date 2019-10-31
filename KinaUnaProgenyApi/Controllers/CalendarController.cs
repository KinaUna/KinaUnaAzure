using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public CalendarController(IDataService dataService, ProgenyDbContext context, AzureNotifications azureNotifications)
        {
            _context = context;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }
        
        // GET api/calendar/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<CalendarItem> calendarList = await _dataService.GetCalendarList(id);
                calendarList = calendarList.Where(c => c.AccessLevel >= accessLevel).ToList();
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
            CalendarItem result = await _dataService.GetCalendarItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return NotFound();
        }

        // POST api/calendar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CalendarItem value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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
            
            _context.CalendarDb.Add(calendarItem);
            await _context.SaveChangesAsync();

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = calendarItem.ProgenyId;
            tItem.AccessLevel = calendarItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Calendar;
            tItem.ItemId = calendarItem.EventId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
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

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);
            await _dataService.SetCalendarItem(calendarItem.EventId);

            string title = "Calendar item added for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new calendar item for " + prog.NickName;

            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            return Ok(calendarItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] CalendarItem value)
        {
            CalendarItem calendarItem = await _context.CalendarDb.SingleOrDefaultAsync(c => c.EventId == id);
            if (calendarItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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

            _context.CalendarDb.Update(calendarItem);
            await _context.SaveChangesAsync();

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == calendarItem.EventId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar);
            if (tItem != null && calendarItem.StartTime.HasValue && calendarItem.EndTime.HasValue)
            {
                tItem.ProgenyTime = calendarItem.StartTime.Value;
                tItem.AccessLevel = calendarItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }
            await _dataService.SetCalendarItem(calendarItem.EventId);

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
            string title = "Calendar edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a calendar item for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(calendarItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.SingleOrDefaultAsync(c => c.EventId == id);
            if (calendarItem != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == calendarItem.ProgenyId);
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

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == calendarItem.EventId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.CalendarDb.Remove(calendarItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveCalendarItem(calendarItem.EventId, calendarItem.ProgenyId);

                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
                string title = "Calendar item deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a calendar item for " + prog.NickName + ". Event: " + calendarItem.Title;
                tItem.AccessLevel = 0;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

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
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess != null || progenyId == Constants.DefaultChildId)
            {
                List<CalendarItem> model = await _dataService.GetCalendarList(progenyId);
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
            CalendarItem result = await _dataService.GetCalendarItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null)
            {
                List<CalendarItem> calendarList = await _dataService.GetCalendarList(id);
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
