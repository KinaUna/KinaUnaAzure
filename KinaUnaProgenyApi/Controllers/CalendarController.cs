using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
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

        public CalendarController(ProgenyDbContext context)
        {
            _context = context;

        }
        
        // GET api/calendar/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
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
            CalendarItem result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
            tItem.CreatedBy = userinfo?.UserId ?? "Unknown";
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

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
            }
            return Ok(calendarItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.SingleOrDefaultAsync(c => c.EventId == id);
            if (calendarItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == calendarItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to edit calendar items for this child.
                    string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
                }

                _context.CalendarDb.Remove(calendarItem);
                await _context.SaveChangesAsync();
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
            UserAccess userAccess = _context.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess != null || progenyId == Constants.DefaultChildId)
            {
                List<CalendarItem> model = await _context.CalendarDb
                    .Where(e => e.ProgenyId == progenyId && e.EndTime > DateTime.UtcNow && e.AccessLevel >= accessLevel).ToListAsync();
                model = model.OrderBy(e => e.StartTime).ToList();
                model = model.Take(5).ToList();

                return Ok(model);
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            CalendarItem result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == result.ProgenyId);
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
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == id);
            if (userAccess != null)
            {
                List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
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
