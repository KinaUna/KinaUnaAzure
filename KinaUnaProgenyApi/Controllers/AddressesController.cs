using System;
using System.Collections.Generic;
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
    public class AddressesController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public AddressesController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/addresses
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Address> resultList = await _context.AddressDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        
        // GET api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddressItem(int id)
        {
            Address result = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(n => n.AddressId == id);

            return Ok(result);
        }

        // POST api/addresses
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Address value)
        {
            Address addressItem = new Address();
            addressItem.AddressLine1 = value.AddressLine1;
            addressItem.AddressLine2 = value.AddressLine2;
            addressItem.City = value.City;
            addressItem.Country = value.Country;
            addressItem.PostalCode = value.PostalCode;
            addressItem.State = value.State;

            _context.AddressDb.Add(addressItem);
            await _context.SaveChangesAsync();

            return Ok(addressItem);
        }

        // PUT api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Address value)
        {
            Address addressItem = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            if (addressItem == null)
            {
                return NotFound();
            }

            addressItem.AddressLine1 = value.AddressLine1;
            addressItem.AddressLine2 = value.AddressLine2;
            addressItem.City = value.City;
            addressItem.Country = value.Country;
            addressItem.PostalCode = value.PostalCode;
            addressItem.State = value.State;

            _context.AddressDb.Update(addressItem);
            await _context.SaveChangesAsync();

            return Ok(addressItem);
        }

        // DELETE api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Address addressItem = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            if (addressItem != null)
            {
                _context.AddressDb.Remove(addressItem);
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
            
            HttpClient addressesHttpClient = new HttpClient();
            
            addressesHttpClient.BaseAddress = new Uri("https://kinauna.com");
            addressesHttpClient.DefaultRequestHeaders.Accept.Clear();
            addressesHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string addressesApiPath = "/api/azureexport/addressexport";
            var addressesUri = "https://kinauna.com" + addressesApiPath;

            var addressesResponseString = await addressesHttpClient.GetStringAsync(addressesUri);

            List<Address> addressesList = JsonConvert.DeserializeObject<List<Address>>(addressesResponseString);
            List<Address> addressesItems = new List<Address>();
            foreach (Address adr in addressesList)
            {
                Address addressItem = new Address();
                addressItem.AddressLine1 = adr.AddressLine1;
                addressItem.AddressLine2 = adr.AddressLine2;
                addressItem.City = adr.City;
                addressItem.Country = adr.Country;
                addressItem.PostalCode = adr.PostalCode;
                addressItem.State = adr.State;
                await _context.AddressDb.AddAsync(addressItem);
                addressesItems.Add(addressItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(addressesItems);
        }
    }
}
