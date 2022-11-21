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
    [Route("[controller]")]
    public class PageTextsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly ITextTranslationService _textTranslationsService;
        
        public PageTextsController(IUserInfoService userInfoService, ITextTranslationService textTranslationsService)
        {
            _userInfoService = userInfoService;
            _textTranslationsService = textTranslationsService;
        }

        [AllowAnonymous]
        [HttpGet("[action]/{title}/{page}/{languageId}")]
        public async Task<IActionResult> ByTitle(string title, string page, int languageId)
        {
            KinaUnaText textItem = await _textTranslationsService.GetTextByTitle(title, page, languageId);
            return Ok(textItem);
        }

        [AllowAnonymous]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetTextById(int id)
        {
            KinaUnaText textItem = await _textTranslationsService.GetTextById(id);
            
            return Ok(textItem);
        }

        [AllowAnonymous]
        [HttpGet("[action]/{textId}/{languageId}")]
        public async Task<IActionResult> GetTextByTextId(int textId, int languageId)
        {
            KinaUnaText textItem = await _textTranslationsService.GetTextByTextId(textId, languageId); 
            
            return Ok(textItem);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]/{page}/{languageId}")]
        public async Task<IActionResult> PageTexts(string page, int languageId)
        {

            List<KinaUnaText> texts = await _textTranslationsService.GetPageTextsList(page, languageId);
            
            return Ok(texts);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]/{languageId}")]
        public async Task<IActionResult> GetAllTexts(int languageId)
        {
            List<KinaUnaText> texts = await _textTranslationsService.GetAllPageTextsList(languageId);

            return Ok(texts);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> CheckLanguages()
        {
            await _textTranslationsService.CheckLanguages();
            
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KinaUnaText value)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                KinaUnaText addedText = await _textTranslationsService.AddText(value);

                return Ok(addedText);
            }

            return Unauthorized();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] KinaUnaText value)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                KinaUnaText updatedText = await _textTranslationsService.UpdateText(id, value);
                if (updatedText != null)
                {
                    return Ok(updatedText);
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
                KinaUnaText textItem = await _textTranslationsService.DeleteText(id);
                if (textItem != null)
                {
                    return Ok(textItem);

                }
                return NotFound();
            }

            return Unauthorized();

        }

        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> DeleteSingleItem(int id)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                KinaUnaText textItem = await _textTranslationsService.DeleteSingleText(id);
                if (textItem != null)
                {
                    return Ok(textItem);
                }
            }

            return NotFound();
        }
    }
}
