using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
            List<CalendarItem> model = new List<CalendarItem>();
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
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient calendarHttpClient = new HttpClient();
            
            calendarHttpClient.BaseAddress = new Uri("https://kinauna.com");
            calendarHttpClient.DefaultRequestHeaders.Accept.Clear();
            calendarHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string calendarApiPath = "/api/azureexport/calendarexport";
            var calendarUri = "https://kinauna.com" + calendarApiPath;

            var calendarResponseString = await calendarHttpClient.GetStringAsync(calendarUri);

            List<CalendarItem> calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarResponseString);
            List<CalendarItem> calendarItems = new List<CalendarItem>();
            foreach (CalendarItem cal in calendarList)
            {
                CalendarItem calendarItem = new CalendarItem();
                calendarItem.AccessLevel = cal.AccessLevel;
                calendarItem.Author = cal.Author;
                calendarItem.Notes = cal.Notes;
                calendarItem.ProgenyId = cal.ProgenyId;
                calendarItem.AllDay = cal.AllDay;
                calendarItem.Context = cal.Context;
                calendarItem.Location = cal.Location;
                calendarItem.Title = cal.Title;
                calendarItem.StartTime = cal.StartTime;
                calendarItem.EndTime = cal.EndTime;
                await _context.CalendarDb.AddAsync(calendarItem);
                    calendarItems.Add(calendarItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(calendarItems);
        }
    }
}
