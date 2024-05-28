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
    public class LanguagesController(ILanguageService languagesService, IUserInfoService userInfoService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetAllLanguages()
        {
            List<KinaUnaLanguage> languagesList = await languagesService.GetAllLanguages();
            return Ok(languagesList);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]/{languageId:int}")]
        public async Task<IActionResult> GetLanguage(int languageId)
        {
            KinaUnaLanguage language = await languagesService.GetLanguage(languageId);
            return Ok(language);
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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
