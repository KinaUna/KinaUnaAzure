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
    public class LocationsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public LocationsController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/locations
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Location> resultList = await _context.LocationsDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == id && l.AccessLevel >= accessLevel).ToListAsync();
            if (locationsList.Any())
            {
                return Ok(locationsList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/locations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);

            return Ok(result);
        }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Location value)
        {
            Location location = new Location();
            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;
            
            _context.LocationsDb.Add(location);
            await _context.SaveChangesAsync();

            return Ok(location);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Location value)
        {
            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location == null)
            {
                return NotFound();
            }

            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;

            _context.LocationsDb.Update(location);
            await _context.SaveChangesAsync();

            return Ok(location);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location != null)
            {
                _context.LocationsDb.Remove(location);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }


        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);

            return Ok(result);
        }
    }
}
