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
    public class SleepController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public SleepController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/sleep
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Sleep> resultList = await _context.SleepDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == id && s.AccessLevel >= accessLevel).ToListAsync();
            if (sleepList.Any())
            {
                return Ok(sleepList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/sleep/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSleepItem(int id)
        {
            Sleep result = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);

            return Ok(result);
        }

        // POST api/sleep
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Sleep value)
        {
            Sleep sleepItem = new Sleep();
            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = DateTime.UtcNow;

            _context.SleepDb.Add(sleepItem);
            await _context.SaveChangesAsync();

            return Ok(sleepItem);
        }

        // PUT api/sleep/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Sleep value)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem == null)
            {
                return NotFound();
            }

            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = value.CreatedDate;

            _context.SleepDb.Update(sleepItem);
            await _context.SaveChangesAsync();

            return Ok(sleepItem);
        }

        // DELETE api/sleep/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem != null)
            {
                _context.SleepDb.Remove(sleepItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient sleepHttpClient = new HttpClient();
            
            sleepHttpClient.BaseAddress = new Uri("https://kinauna.com");
            sleepHttpClient.DefaultRequestHeaders.Accept.Clear();
            sleepHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string sleepApiPath = "/api/azureexport/sleepexport";
            var sleepUri = "https://kinauna.com" + sleepApiPath;

            var sleepResponseString = await sleepHttpClient.GetStringAsync(sleepUri);

            List<Sleep> sleepList = JsonConvert.DeserializeObject<List<Sleep>>(sleepResponseString);
            List<Sleep> sleepItems = new List<Sleep>();
            foreach (Sleep value in sleepList)
            {
                Sleep sleepItem = new Sleep();
                sleepItem.AccessLevel = value.AccessLevel;
                sleepItem.Author = value.Author;
                sleepItem.SleepNotes = value.SleepNotes;
                sleepItem.SleepRating = value.SleepRating;
                sleepItem.ProgenyId = value.ProgenyId;
                sleepItem.SleepStart = value.SleepStart;
                sleepItem.SleepEnd = value.SleepEnd;
                sleepItem.CreatedDate = value.CreatedDate;
                await _context.SleepDb.AddAsync(sleepItem);
                sleepItems.Add(sleepItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(sleepItems);
        }
    }
}
