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

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient locationsHttpClient = new HttpClient();
            
            locationsHttpClient.BaseAddress = new Uri("https://kinauna.com");
            locationsHttpClient.DefaultRequestHeaders.Accept.Clear();
            locationsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string locationsApiPath = "/api/azureexport/locationsexport";
            var locationsUri = "https://kinauna.com" + locationsApiPath;

            var locationsResponseString = await locationsHttpClient.GetStringAsync(locationsUri);

            List<Location> locationsList = JsonConvert.DeserializeObject<List<Location>>(locationsResponseString);
            List<Location> addedLocations = new List<Location>();
            foreach (Location loc in locationsList)
            {
                Location tempLocation = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == loc.LocationId);
                if (tempLocation == null)
                {
                    Location newLocation = new Location();
                    newLocation.AccessLevel = loc.AccessLevel;
                    newLocation.Author = loc.Author;
                    newLocation.City = loc.City;
                    newLocation.Country = loc.Country;
                    newLocation.County = loc.County;
                    newLocation.Date = loc.Date;
                    newLocation.DateAdded = loc.DateAdded;
                    newLocation.District = loc.District;
                    newLocation.HouseNumber = loc.HouseNumber;
                    newLocation.Latitude = loc.Latitude;
                    newLocation.Longitude = loc.Longitude;
                    newLocation.Name = loc.Name;
                    newLocation.Notes = loc.Notes;
                    newLocation.PostalCode = loc.PostalCode;
                    newLocation.ProgenyId = loc.ProgenyId;
                    newLocation.State = loc.State;
                    newLocation.StreetName = loc.StreetName;
                    newLocation.Tags = loc.Tags;

                    await _context.LocationsDb.AddAsync(newLocation);
                    addedLocations.Add(newLocation);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(addedLocations);
        }
    }
}
