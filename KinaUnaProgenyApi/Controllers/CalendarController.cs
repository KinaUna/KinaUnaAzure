using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
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
        // GET api/calendar
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<CalendarItem> resultList = await _context.CalendarDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/calendar/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
            if (calendarList.Any())
            {
                return Ok(calendarList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/calendar/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCalendarItem(int id)
        {
            CalendarItem result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);

            return Ok(result);
        }

        // POST api/calendar
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CalendarItem value)
        {
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

            return Ok(calendarItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.SingleOrDefaultAsync(c => c.EventId == id);
            if (calendarItem != null)
            {
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
            List<CalendarItem> model;
            model = await _context.CalendarDb
                .Where(e => e.ProgenyId == progenyId && e.EndTime > DateTime.UtcNow && e.AccessLevel >= accessLevel).ToListAsync();
            model = model.OrderBy(e => e.StartTime).ToList();
            model = model.Take(5).ToList();

            return Ok(model);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            CalendarItem result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);

            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
            if (calendarList.Any())
            {
                return Ok(calendarList);
            }
            else
            {
                return NotFound();
            }

        }
    }
}
