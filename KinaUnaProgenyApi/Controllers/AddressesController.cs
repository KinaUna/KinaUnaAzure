using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Addresses.
    /// </summary>
    /// <param name="locationService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AddressesController(ILocationService locationService) : ControllerBase
    {
        // Todo: Security check, verify that users can view/change addresses.

        /// <summary>
        /// Retrieves the Address with a given id.
        /// </summary>
        /// <param name="id">The AddressId of the Address entity to retrieve.</param>
        /// <returns>Address entity with the id.</returns>
        // GET api/addresses/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAddressItem(int id)
        {
            Address result = await locationService.GetAddressItem(id);
            return Ok(result);
        }

        /// <summary>
        /// Adds a new Address entity to the database.
        /// </summary>
        /// <param name="value">The Address object to add.</param>
        /// <returns>The added Address object</returns>
        // POST api/addresses
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Address value)
        {
            Address addressItem = await locationService.AddAddressItem(value);

            return Ok(addressItem);
        }

        /// <summary>
        /// Updates an existing Address entity in the database.
        /// </summary>
        /// <param name="id">The AddressId of the Address to update.</param>
        /// <param name="value">The Address object containing the properties to update.</param>
        /// <returns>The updated Address object.</returns>
        // PUT api/addresses/5
        [HttpPut("{id:int}")]
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

        /// <summary>
        /// Deletes an Address entity from the database.
        /// </summary>
        /// <param name="id">The AddressId of the Address entity.</param>
        /// <returns>NoContentResult.</returns>
        // DELETE api/addresses/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Address addressItem = await locationService.GetAddressItem(id);
            if (addressItem == null) return NotFound();

            await locationService.RemoveAddressItem(id);

            return NoContent();

        }
    }
}
