using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AddressesController : ControllerBase
    {
        // Todo: Security check, verify that users can view/change addresses.
        private readonly ILocationService _locationService;
        public AddressesController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        // GET api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddressItem(int id)
        {
            Address result = await _locationService.GetAddressItem(id);
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
            
            addressItem = await _locationService.AddAddressItem(addressItem);
            
            return Ok(addressItem);
        }

        // PUT api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Address value)
        {
            Address addressItem = await _locationService.GetAddressItem(id);
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
            
            addressItem = await _locationService.UpdateAddressItem(addressItem);
            
            return Ok(addressItem);
        }

        // DELETE api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Address addressItem = await _locationService.GetAddressItem(id);
            if (addressItem != null)
            {
                await _locationService.RemoveAddressItem(id); 
                
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
