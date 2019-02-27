using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TimeLineItem = KinaUnaProgenyApi.Models.TimeLineItem;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TimeLineController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public TimeLineController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/timeline
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<TimeLineItem> resultList = await _context.TimeLineDb.AsNoTracking().ToListAsync();
            
            return Ok(resultList);
        }

        // GET api/timeline/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel).ToListAsync();
            if (timeLineList.Any())
            {
                return Ok(timeLineList);
            }
            else
            {
                return Ok(new List<TimeLineItem>());
            }

        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start =0)
        {
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToListAsync();
            if (timeLineList.Any())
            {
                timeLineList.Reverse();

                return Ok(timeLineList.Skip(start).Take(count));
            }
            else
            {
                return Ok(new List<TimeLineItem>());
            }

        }

        // GET api/timeline/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTimeLineItem(int id)
        {
            TimeLineItem result = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(u => u.TimeLineId == id);
            
            return Ok(result);
        }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TimeLineItem value)
        {
            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.ProgenyId = value.ProgenyId;
            timeLineItem.AccessLevel = value.AccessLevel;
            timeLineItem.CreatedBy = value.CreatedBy;
            timeLineItem.CreatedTime = value.CreatedTime;
            timeLineItem.ItemId = value.ItemId;
            timeLineItem.ItemType = value.ItemType;
            timeLineItem.ProgenyTime = value.ProgenyTime;

            _context.TimeLineDb.Add(timeLineItem);
            await _context.SaveChangesAsync();

            return Ok(timeLineItem);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TimeLineItem value)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.TimeLineId == id);
            if (timeLineItem == null)
            {
                return NotFound();
            }

            timeLineItem.ProgenyId = value.ProgenyId;
            timeLineItem.AccessLevel = value.AccessLevel;
            timeLineItem.CreatedBy = value.CreatedBy;
            timeLineItem.CreatedTime = value.CreatedTime;
            timeLineItem.ItemId = value.ItemId;
            timeLineItem.ItemType = value.ItemType;
            timeLineItem.ProgenyTime = value.ProgenyTime;
            
            _context.TimeLineDb.Update(timeLineItem);
            await _context.SaveChangesAsync();

            return Ok(timeLineItem);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.TimeLineId == id);
            if (timeLineItem != null)
            {
                _context.TimeLineDb.Remove(timeLineItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}/{year}/{month}/{day}")]
        public async Task<IActionResult> ProgenyLatestMobile(int id, int accessLevel = 5, int count = 5, int start = 0, int year = 0, int month = 0, int day = 0)
        {
            DateTime startDate;
            if (year != 0 && month != 0 && day != 0)
            {
                startDate = new DateTime(year, month, day, 23, 59, 59, DateTimeKind.Utc);
               
            }
            else
            {
                startDate = DateTime.UtcNow;
            }
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < startDate).OrderBy(t => t.ProgenyTime).ToListAsync();
            if (timeLineList.Any())
            {
                timeLineList.Reverse();

                return Ok(timeLineList.Skip(start).Take(count));
            }
            else
            {
                return Ok(new List<TimeLineItem>());
            }

        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {

            HttpClient timeLineHttpClient = new HttpClient();

            timeLineHttpClient.BaseAddress = new Uri("https://kinauna.com");
            timeLineHttpClient.DefaultRequestHeaders.Accept.Clear();
            timeLineHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string timeLineApiPath = "/api/azureexport/timelineexport";
            var timeLineUri = "https://kinauna.com" + timeLineApiPath;

            var timeLineResponseString = await timeLineHttpClient.GetStringAsync(timeLineUri);

            List<TimeLineItem> timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(timeLineResponseString);
            List<TimeLineItem> timeLineItems = new List<TimeLineItem>();
            foreach (TimeLineItem value in timeLineList)
            {
                TimeLineItem timeLineItem = new TimeLineItem();
                timeLineItem.ProgenyId = value.ProgenyId;
                timeLineItem.AccessLevel = value.AccessLevel;
                timeLineItem.CreatedBy = value.CreatedBy;
                timeLineItem.CreatedTime = value.CreatedTime;
                timeLineItem.ItemId = value.ItemId;
                timeLineItem.ItemType = value.ItemType;
                timeLineItem.ProgenyTime = value.ProgenyTime;
                await _context.TimeLineDb.AddAsync(timeLineItem);
                timeLineItems.Add(timeLineItem);

            }
            await _context.SaveChangesAsync();

            return Ok(timeLineItems);
        }
    }
}
