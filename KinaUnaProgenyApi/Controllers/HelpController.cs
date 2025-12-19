using System.Threading.Tasks;
using KinaUna.Data.Models.Support;
using KinaUnaProgenyApi.Services.Support;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Controller for retrieving help content.
    /// </summary>
    /// <param name="helpContentService"></param>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController(IHelpContentService helpContentService) : ControllerBase
    {
        /// <summary>
        /// Gets help content for a specific page and element in the specified language.
        /// </summary>
        /// <param name="page">The page identifier.</param>
        /// <param name="element">The element identifier. Empty string for page-level help.</param>
        /// <param name="languageId">The language identifier.</param>
        /// <returns>HelpContent object.</returns>
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetHelpContent(string page, string element, int languageId)
        {
            HelpContent helpContent = await helpContentService.GetHelpContent(page, element, languageId);
            
            return Ok(helpContent);
        }
    }
}
