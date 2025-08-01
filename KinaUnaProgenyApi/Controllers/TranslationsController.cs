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
    /// API endpoints for translations.
    /// </summary>
    /// <param name="userInfoService"></param>
    /// <param name="textTranslationService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationsController(IUserInfoService userInfoService, ITextTranslationService textTranslationService) : ControllerBase
    {
        /// <summary>
        /// Get all translations for a specific language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language to get translations for.</param>
        /// <returns>List of all TextTranslation entities for the language.</returns>
        [HttpGet("[action]/{languageId:int}")]
        public async Task<IActionResult> GetAllTranslations(int languageId)
        {
            List<TextTranslation> translations = await textTranslationService.GetAllTranslations(languageId);

            return Ok(translations);
        }

        /// <summary>
        /// Get a specific translation by id.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation item to get.</param>
        /// <returns>TextTranslation object with the given Id.</returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetTranslationById(int id)
        {
            TextTranslation translation = await textTranslationService.GetTranslationById(id);

            return Ok(translation);
        }

        /// <summary>
        /// Gets a translation by Word, Page and LanguageId. If the translation does not exist, it will be created with the Word as the Translation.
        /// </summary>
        /// <param name="word">The Word property of the TextTranslation to get.</param>
        /// <param name="page">The page the Word appears on.</param>
        /// <param name="languageId">The LanguageId to translate the Word into.</param>
        /// <returns>The TextTranslation.</returns>
        [HttpGet("[action]/{word}/{page}/{languageId:int}")]
        public async Task<IActionResult> GetTranslationByWord(string word, string page, int languageId)
        {
            TextTranslation translation = await textTranslationService.GetTranslationByWord(word, page, languageId);
            if (translation != null) return Ok(translation);

            TextTranslation translationItem = new()
            {
                LanguageId = languageId,
                Word = word,
                Page = page,
                Translation = word
            };

            string userId = User.GetUserId();

            if (await userInfoService.IsAdminUserId(userId))
            {
                translation = await textTranslationService.AddTranslation(translationItem);
            }
            else
            {
                translation = translationItem;
            }
            return Ok(translation);
        }

        /// <summary>
        /// Gets a list of all translation for a given page.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language to get translations for.</param>
        /// <param name="page">The page to get translations for.</param>
        /// <returns>List of TextTranslations.</returns>
        [HttpGet]
        [Route("[action]/{languageId:int}/{page}")]
        public async Task<IActionResult> PageTranslations(int languageId, string page)
        {
            List<TextTranslation> translations = await textTranslationService.GetPageTranslations(languageId, page);

            return Ok(translations);
        }

        /// <summary>
        /// Adds a new TextTranslation. If the translation already exists, it will not be added.
        /// </summary>
        /// <param name="value">TextTranslation to add.</param>
        /// <returns>The added TextTranslation.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TextTranslation value)
        {
            if (value.LanguageId == 0)
            {
                value.LanguageId = 1;
            }

            TextTranslation existingTranslation = await textTranslationService.GetTranslationByWord(value.Word, value.Page, value.LanguageId);
            if (existingTranslation == null)
            {
                value = await textTranslationService.AddTranslation(value);
            }

            return Ok(value);
        }

        /// <summary>
        /// Updates a TextTranslation. Only Admin users can update translations.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation.</param>
        /// <param name="value">TextTranslation object with the properties to update.</param>
        /// <returns>The updated TextTranslation object.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] TextTranslation value)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            TextTranslation translation = await textTranslationService.UpdateTranslation(id, value);
            if (translation != null)
            {
                return Ok(translation);
            }

            return NotFound();

        }

        /// <summary>
        /// Deletes a TextTranslation and the translations in other languages as well. Only Admin users can delete translations.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = User.GetUserId();

            if (!await userInfoService.IsAdminUserId(userId)) return Unauthorized();

            TextTranslation translation = await textTranslationService.DeleteTranslation(id);
            if (translation != null)
            {
                return Ok(translation);
            }
            return NotFound();

        }

        /// <summary>
        /// Deletes a single TextTranslation, but not the translations in other languages.
        /// </summary>
        /// <param name="id">The Id of the TextTranslation entity to delete.</param>
        /// <returns>The deleted TextTranslation object.</returns>
        [HttpDelete("[action]/{id:int}")]
        public async Task<IActionResult> DeleteSingleItem(int id)
        {
            TextTranslation translation = await textTranslationService.DeleteSingleTranslation(id);
            if (translation != null)
            {
                return Ok(translation);
            }

            return NotFound();
        }

    }
}
