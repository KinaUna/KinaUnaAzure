using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for languages.
    /// </summary>
    /// <param name="languagesService"></param>
    /// <param name="userInfoService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LanguagesController(ILanguageService languagesService, IUserInfoService userInfoService) : ControllerBase
    {
        /// <summary>
        /// Retrieves all language supported by the application.
        /// </summary>
        /// <returns>List of all KinaUnaLanguage entities in the database.</returns>
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetAllLanguages()
        {
            List<KinaUnaLanguage> languagesList = await languagesService.GetAllLanguages();
            return Ok(languagesList);
        }

        /// <summary>
        /// Retrieves a specific language by languageId.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage entity to get.</param>
        /// <returns>The KinaUnaLanguage object with the provided id.</returns>
        [HttpGet]
        [Route("[action]/{languageId:int}")]
        public async Task<IActionResult> GetLanguage(int languageId)
        {
            KinaUnaLanguage language = await languagesService.GetLanguage(languageId);
            return Ok(language);
        }

        /// <summary>
        /// Adds a new language to the database.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object to add.</param>
        /// <returns>The added KinaUnaLanguage object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddLanguage([FromBody] KinaUnaLanguage language)
        {
            string userId = User.GetUserId();
            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            language.Name = language.Name?.Trim();
            await languagesService.AddLanguage(language);

            return Ok(language);
        }

        /// <summary>
        /// Updates an existing language in the database.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage entity to update.</param>
        /// <param name="value">KinaUnaLanguage object with the properties to update.</param>
        /// <returns>The updated KinaUnaLanguage object.</returns>
        [HttpPut]
        [Route("[action]/{languageId:int}")]
        public async Task<IActionResult> UpdateLanguage(int languageId, [FromBody] KinaUnaLanguage value)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            KinaUnaLanguage language = await languagesService.GetLanguage(languageId);
            if (language == null)
            {
                return NotFound();
            }

            language = await languagesService.UpdateLanguage(value);

            return Ok(language);

        }

        /// <summary>
        /// Deletes a language with the given id from the database.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage entity to delete.</param>
        /// <returns>The deleted KinaUnaLanguage object.</returns>
        [HttpDelete]
        [Route("[action]/{languageId:int}")]
        public async Task<IActionResult> DeleteLanguage(int languageId)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            KinaUnaLanguage deletedLanguage = await languagesService.DeleteLanguage(languageId);
            if (deletedLanguage == null)
            {
                return NotFound();
            }

            return Ok(deletedLanguage);

        }
    }
}
