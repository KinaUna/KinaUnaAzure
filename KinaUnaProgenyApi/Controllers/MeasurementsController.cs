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
    public class MeasurementsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public MeasurementsController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/measurements
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Measurement> resultList = await _context.MeasurementsDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/measurements/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == id && m.AccessLevel >= accessLevel).ToListAsync();
            if (measurementsList.Any())
            {
                return Ok(measurementsList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/measurements/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMeasurementItem(int id)
        {
            Measurement result = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);

            return Ok(result);
        }

        // POST api/measurements
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Measurement value)
        {
            Measurement measurementItem = new Measurement();
            measurementItem.AccessLevel = value.AccessLevel;
            measurementItem.Author = value.Author;
            measurementItem.Date = value.Date;
            measurementItem.Circumference = value.Circumference;
            measurementItem.ProgenyId = value.ProgenyId;
            measurementItem.EyeColor = value.EyeColor;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.HairColor = value.HairColor;
            measurementItem.Height = value.Height;
            measurementItem.Weight = value.Weight;
            
            _context.MeasurementsDb.Add(measurementItem);
            await _context.SaveChangesAsync();

            return Ok(measurementItem);
        }

        // PUT api/measurement/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Measurement value)
        {
            Measurement measurementItem = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurementItem == null)
            {
                return NotFound();
            }

            measurementItem.AccessLevel = value.AccessLevel;
            measurementItem.Author = value.Author;
            measurementItem.Date = value.Date;
            measurementItem.Circumference = value.Circumference;
            measurementItem.ProgenyId = value.ProgenyId;
            measurementItem.EyeColor = value.EyeColor;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.HairColor = value.HairColor;
            measurementItem.Height = value.Height;
            measurementItem.Weight = value.Weight;

            _context.MeasurementsDb.Update(measurementItem);
            await _context.SaveChangesAsync();

            return Ok(measurementItem);
        }

        // DELETE api/measurements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Measurement measurementItem = await _context.MeasurementsDb.SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (measurementItem != null)
            {
                _context.MeasurementsDb.Remove(measurementItem);
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
            
            HttpClient measurementsHttpClient = new HttpClient();
            
            measurementsHttpClient.BaseAddress = new Uri("https://kinauna.com");
            measurementsHttpClient.DefaultRequestHeaders.Accept.Clear();
            measurementsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string measurementsApiPath = "/api/azureexport/measurementsexport";
            var measurementsUri = "https://kinauna.com" + measurementsApiPath;

            var measurementsResponseString = await measurementsHttpClient.GetStringAsync(measurementsUri);

            List<Measurement> measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(measurementsResponseString);
            List<Measurement> measurementsItems = new List<Measurement>();
            foreach (Measurement value in measurementsList)
            {
                Measurement measurementItem = new Measurement();
                measurementItem.AccessLevel = value.AccessLevel;
                measurementItem.Author = value.Author;
                measurementItem.Date = value.Date;
                measurementItem.Circumference = value.Circumference;
                measurementItem.ProgenyId = value.ProgenyId;
                measurementItem.EyeColor = value.EyeColor;
                measurementItem.CreatedDate = value.CreatedDate;
                measurementItem.HairColor = value.HairColor;
                measurementItem.Height = value.Height;
                measurementItem.Weight = value.Weight;
                await _context.MeasurementsDb.AddAsync(measurementItem);
                measurementsItems.Add(measurementItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(measurementsItems);
        }
    }
}
