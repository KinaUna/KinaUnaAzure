using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
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
    public class TranslationsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly ITextTranslationService _textTranslationService;

        public TranslationsController(IUserInfoService userInfoService, ITextTranslationService textTranslationService)
        {
            _userInfoService = userInfoService;
            _textTranslationService = textTranslationService;
        }

        [AllowAnonymous]
        [HttpGet("[action]/{languageId}")]
        public async Task<IActionResult> GetAllTranslations(int languageId)
        {
            List<TextTranslation> translations = await _textTranslationService.GetAllTranslations(languageId);
            
            return Ok(translations);
        }

        [AllowAnonymous]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetTranslationById(int id)
        {
            TextTranslation translation = await _textTranslationService.GetTranslationById(id);
            
            return Ok(translation);
        }

        [AllowAnonymous]
        [HttpGet("[action]/{word}/{page}/{languageId}")]
        public async Task<IActionResult> GetTranslationByWord(string word, string page, int languageId)
        {
            TextTranslation translation = await _textTranslationService.GetTranslationByWord(word, page, languageId);
            if (translation == null)
            {
                TextTranslation translationItem = new TextTranslation();
                translationItem.LanguageId = languageId;
                translationItem.Word = word;
                translationItem.Page = page;
                translationItem.Translation = word;

                string userId = User.GetUserId();

                if (await _userInfoService.IsAdminUserId(userId))
                {
                    translation = await _textTranslationService.AddTranslation(translationItem);
                }
                else
                {
                    translation = translationItem;
                }
            }
            return Ok(translation);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]/{languageId}/{page}")]
        public async Task<IActionResult> PageTranslations(int languageId, string page)
        {
            List<TextTranslation> translations = await _textTranslationService.GetPageTranslations(languageId, page);

            return Ok(translations);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TextTranslation value)
        {
            if (value.LanguageId == 0)
            {
                value.LanguageId = 1;
            }

            TextTranslation existingTranslation = await _textTranslationService.GetTranslationByWord(value.Word, value.Page, value.LanguageId);
            if (existingTranslation == null)
            {
                value = await _textTranslationService.AddTranslation(value);
            }
            
            return Ok(value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TextTranslation value)
        {
            string userId = User.GetUserId();
            
            if (await _userInfoService.IsAdminUserId(userId))
            {
                TextTranslation translation = await _textTranslationService.UpdateTranslation(id, value);
                if (translation != null)
                {
                    return Ok(translation);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                TextTranslation translation = await _textTranslationService.DeleteTranslation(id);
                if (translation != null)
                {
                    return Ok(translation);
                }
                return NotFound();
            }

            return Unauthorized();

        }

        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> DeleteSingleItem(int id)
        {
            TextTranslation translation = await _textTranslationService.DeleteSingleTranslation(id);
            if (translation != null)
            {
                return Ok(translation);
            }

            return NotFound();
        }
        
    }
}
