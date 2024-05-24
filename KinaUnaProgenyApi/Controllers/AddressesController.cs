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
    public class AddressesController(ILocationService locationService) : ControllerBase
    {
        // Todo: Security check, verify that users can view/change addresses.

        // GET api/addresses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddressItem(int id)
        {
            Address result = await locationService.GetAddressItem(id);
            return Ok(result);
        }

        // POST api/addresses
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Address value)
        {
            Address addressItem = await locationService.AddAddressItem(value);

            return Ok(addressItem);
        }

        // PUT api/addresses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Address value)
        {
            Address addressItem = await locationService.GetAddressItem(id);
            if (addressItem == null)
            {
                return NotFound();
            }

            addressItem = await locationService.UpdateAddressItem(value);

            return Ok(addressItem);
        }

        // DELETE api/addresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Address addressItem = await locationService.GetAddressItem(id);
            if (addressItem != null)
            {
                await locationService.RemoveAddressItem(id);

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
