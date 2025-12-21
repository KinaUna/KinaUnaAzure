using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Support;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.Support;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Controller for managing help content.
    /// </summary>
    /// <param name="helpContentService">Service for help content operations.</param>
    /// <param name="userInfoService">Service for user information operations.</param>
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class HelpController(IHelpContentService helpContentService, IUserInfoService userInfoService) : ControllerBase
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

        [HttpGet]
        [Route("[action]/{helpContentId}")]
        public async Task<IActionResult> GetHelpContentById(int helpContentId)
        {
            HelpContent helpContent = await helpContentService.GetHelpContentById(helpContentId);
            return Ok(helpContent);
        }

        /// <summary>
        /// Adds a new help content entry to the system.
        /// </summary>
        /// <param name="helpContent">The help content to add. Must not be null.</param>
        /// <returns>An <see cref="IActionResult"/> that represents the result of the operation. Returns 200 OK with the added
        /// help content if successful; otherwise, returns 401 Unauthorized if the user is not an administrator.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddHelpContent([FromBody] HelpContent helpContent)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            HelpContent addedHelpContent = await helpContentService.AddHelpContent(helpContent, currentUserInfo);
            return Ok(addedHelpContent);
        }

        /// <summary>
        /// Updates the help content with the specified information.
        /// </summary>
        /// <remarks>Only users with Kina Una administrator privileges are authorized to perform this
        /// operation. Unauthorized users will receive an HTTP 401 response.</remarks>
        /// <param name="helpContent">The help content to update. Must contain valid and complete help content data.</param>
        /// <returns>An <see cref="IActionResult"/> that represents the result of the operation. Returns an HTTP 200 response
        /// with the updated help content if successful; otherwise, returns an appropriate error response.</returns>
        [HttpPut]
        [Route("[action]")]
        public async Task<IActionResult> UpdateHelpContent([FromBody] HelpContent helpContent)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            HelpContent updatedHelpContent = await helpContentService.UpdateHelpContent(helpContent, currentUserInfo);
            return Ok(updatedHelpContent);
        }

        /// <summary>
        /// Deletes the specified help content item by its unique identifier.
        /// </summary>
        /// <remarks>Only users with Kina Una administrator privileges are authorized to delete help
        /// content. If the specified help content does not exist, the response will indicate success with a null
        /// value.</remarks>
        /// <param name="helpContentId">The unique identifier of the help content item to delete.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the deleted help content if the operation succeeds; otherwise, an
        /// <see cref="UnauthorizedResult"/> if the user is not authorized.</returns>
        [HttpDelete]
        [Route("[action]/{helpContentId}")]
        public async Task<IActionResult> DeleteHelpContent(int helpContentId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            HelpContent deletedHelpContent = await helpContentService.DeleteHelpContent(helpContentId, currentUserInfo);
            return Ok(deletedHelpContent);
        }
    }
}