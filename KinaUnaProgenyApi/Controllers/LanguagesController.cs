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
    public class LanguagesController : ControllerBase
    {
        private readonly ILanguageService _languagesService;
        private readonly IUserInfoService _userInfoService;

        public LanguagesController(ILanguageService languagesService, IUserInfoService userInfoService)
        {
            _languagesService = languagesService;
            _userInfoService = userInfoService;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetAllLanguages()
        {
            List<KinaUnaLanguage> languagesList = await _languagesService.GetAllLanguages();
            return Ok(languagesList);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("[action]/{languageId}")]
        public async Task<IActionResult> GetLanguage(int languageId)
        {
            KinaUnaLanguage language = await _languagesService.GetLanguage(languageId);
            return Ok(language);
        }

        [Authorize]
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddLanguage([FromBody] KinaUnaLanguage language)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                language.Name = language.Name?.Trim();
                await _languagesService.AddLanguage(language);

                return Ok(language);
            }

            return Unauthorized();
        }

        [Authorize]
        [HttpPut]
        [Route("[action]/{languageId}")]
        public async Task<IActionResult> UpdateLanguage(int languageId, [FromBody] KinaUnaLanguage value)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                KinaUnaLanguage language = await _languagesService.GetLanguage(languageId);
                if (language == null)
                {
                    return NotFound();
                }

                language.Name = value.Name?.Trim();
                language.Code = value.Code;
                language.Icon = value.Icon;
                language = await _languagesService.UpdateLanguage(language);

                return Ok(language);
            }
                        
            return Unauthorized();
        }

        [Authorize]
        [HttpDelete]
        [Route("[action]/{languageId}")]
        public async Task<IActionResult> DeleteLanguage(int languageId)
        {
            string userId = User.GetUserId();

            if (await _userInfoService.IsAdminUserId(userId))
            {
                KinaUnaLanguage deletedLanguage = await _languagesService.DeleteLanguage(languageId);
                if (deletedLanguage.Id == -1)
                {
                    return NotFound();
                }

                return Ok(deletedLanguage);
            }
            
            return Unauthorized();
        }
    }
}
