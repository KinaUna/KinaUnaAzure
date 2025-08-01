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
    /// API endpoints for dynamic text content on pages.
    /// </summary>
    /// <param name="userInfoService"></param>
    /// <param name="kinaUnaTextService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PageTextsController(IUserInfoService userInfoService, IKinaUnaTextService kinaUnaTextService) : ControllerBase
    {
        /// <summary>
        /// Gets a KinaUnaText item by title, for a given page, in a given language.
        /// </summary>
        /// <param name="title">The Title property of the KinaUnaText.</param>
        /// <param name="page">The page where the text appears.</param>
        /// <param name="languageId">The LanguageId of the KinaUnaText to get.</param>
        /// <returns>KinaUnaText object with the provided title, page, and languageId properties.</returns>
        [HttpGet("[action]/{title}/{page}/{languageId:int}")]
        public async Task<IActionResult> ByTitle(string title, string page, int languageId)
        {
            KinaUnaText textItem = await kinaUnaTextService.GetTextByTitle(title, page, languageId);
            return Ok(textItem);
        }

        /// <summary>
        /// Gets a KinaUnaText item by id (not TextId).
        /// </summary>
        /// <param name="id">The id property of the KinaUnaText entity to get.</param>
        /// <returns>The KinaUnaText object with the provided id.</returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetTextById(int id)
        {
            KinaUnaText textItem = await kinaUnaTextService.GetTextById(id);

            return Ok(textItem);
        }

        /// <summary>
        /// Gets a KinaUnaText item by TextId and LanguageId.
        /// </summary>
        /// <param name="textId">The TextId of the KinaUnaText to retrieve.</param>
        /// <param name="languageId"> The LanguageId of the KinaUnaText to retrieve.</param>
        /// <returns>The KinaUnaText with the provided TextId and LanguageId.</returns>
        [HttpGet("[action]/{textId:int}/{languageId:int}")]
        public async Task<IActionResult> GetTextByTextId(int textId, int languageId)
        {
            KinaUnaText textItem = await kinaUnaTextService.GetTextByTextId(textId, languageId);

            return Ok(textItem);
        }

        /// <summary>
        /// Gets a list of KinaUnaText items for a given page and language.
        /// </summary>
        /// <param name="page">The page to retrieve KinaUnaText entities for.</param>
        /// <param name="languageId">The LanguageId of the KinaUnaTexts.</param>
        /// <returns>List of KinaUnaText objects that belong to the page.</returns>
        [HttpGet]
        [Route("[action]/{page}/{languageId:int}")]
        public async Task<IActionResult> PageTexts(string page, int languageId)
        {

            List<KinaUnaText> texts = await kinaUnaTextService.GetPageTextsList(page, languageId);

            return Ok(texts);
        }


        /// <summary>
        /// Gets a list of all KinaUnaText items for a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId to retrieve KinaUnaTexts for.</param>
        /// <returns>A list with all the KinaUnaTexts in the provided language.</returns>
        [HttpGet]
        [Route("[action]/{languageId:int}")]
        public async Task<IActionResult> GetAllTexts(int languageId)
        {
            List<KinaUnaText> texts = await kinaUnaTextService.GetAllPageTextsList(languageId);

            return Ok(texts);
        }

        /// <summary>
        /// Finds and fixes missing translations in the KinaUnaText table.
        /// If a text is missing in a language, it will be added with a copy of the first translation found.
        /// </summary>
        /// <returns>Ok</returns>
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> CheckLanguages()
        {
            await kinaUnaTextService.CheckLanguages();

            return Ok();
        }

        /// <summary>
        /// Adds a new KinaUnaText entity to the database.
        /// </summary>
        /// <param name="value">The KinaUnaText entity to add.</param>
        /// <returns>The added KinaUnaText object.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KinaUnaText value)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            KinaUnaText addedText = await kinaUnaTextService.AddText(value);

            return Ok(addedText);

        }

        /// <summary>
        /// Updates a KinaUnaText entity in the database.
        /// </summary>
        /// <param name="id">The id of the KinaUnaText entity to update.</param>
        /// <param name="value">The KinaUnaText object with the updated properties.</param>
        /// <returns>The updated KinaUnaText</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] KinaUnaText value)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            KinaUnaText updatedText = await kinaUnaTextService.UpdateText(id, value);
            if (updatedText != null)
            {
                return Ok(updatedText);
            }

            return NotFound();

        }

        /// <summary>
        /// Deletes a KinaUnaText entity, and all translations with the same TextId, from the database.
        /// </summary>
        /// <param name="id">The id of the KinaUnaText to remove.</param>
        /// <returns>The deleted KinaUnaText object, or NotFound if it doesn't exist.</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            KinaUnaText textItem = await kinaUnaTextService.DeleteText(id);
            if (textItem != null)
            {
                return Ok(textItem);

            }
            return NotFound();

        }

        /// <summary>
        /// Deletes a single KinaUnaText entity from the database.
        /// Other entities with the same TextId and different LanguageId will not be affected.
        /// </summary>
        /// <param name="id">The id of the KinaUnaText to remove.</param>
        /// <returns>The deleted KinaUnaText object, or NotFound there are no entities found with the given id.</returns>
        [HttpDelete("[action]/{id:int}")]
        public async Task<IActionResult> DeleteSingleItem(int id)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return NotFound();

            KinaUnaText textItem = await kinaUnaTextService.DeleteSingleText(id);
            if (textItem != null)
            {
                return Ok(textItem);
            }

            return NotFound();
        }
    }
}
