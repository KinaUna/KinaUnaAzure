using System.Threading.Tasks;
using KinaUna.Data.Contexts;
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
    public class AddressesController : ControllerBase
    {
        // Todo: Security check, verify that users can view/change addresses.
        private readonly IDataService _dataService;
        public AddressesController(ProgenyDbContext context, IDataService dataService)
        {
            _dataService = dataService;
        }
        
        // GET api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddressItem(int id)
        {
            Address result = await _dataService.GetAddressItem(id);
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
            
            addressItem = await _dataService.AddAddressItem(addressItem);
            
            return Ok(addressItem);
        }

        // PUT api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Address value)
        {
            Address addressItem = await _dataService.GetAddressItem(id); // _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
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
            
            addressItem = await _dataService.UpdateAddressItem(addressItem);
            
            return Ok(addressItem);
        }

        // DELETE api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Address addressItem = await _dataService.GetAddressItem(id); // _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            if (addressItem != null)
            {
                await _dataService.RemoveAddressItem(id); 
                
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
