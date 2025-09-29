using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Progeny data.
    /// </summary>
    /// <param name="progenyService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProgenyController(IProgenyService progenyService, IUserInfoService userInfoService) : ControllerBase
    {
        /// <summary>
        /// Gets the Progeny entity with the specified id.
        /// </summary>
        /// <param name="id">The ProgenyId for the Progeny entity to get.</param>
        /// <returns>The Progeny object with the given ProgenyId.</returns>
        // GET api/progeny/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            Progeny result = await progenyService.GetProgeny(id, currentUserInfo);

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();

        }

        /// <summary>
        /// Adds a new Progeny entity to the database.
        /// </summary>
        /// <param name="value">Progeny object to add to the database.</param>
        /// <returns>The added Progeny object.</returns>
        // POST api/progeny
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Progeny value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            Progeny progeny = new()
            {
                Name = value.Name,
                NickName = value.NickName,
                BirthDay = value.BirthDay,
                TimeZone = value.TimeZone,
                Admins = value.Admins
            };
            if (string.IsNullOrEmpty(value.PictureLink))
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            progeny.PictureLink = value.PictureLink;

            if (!progeny.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase))
            {
                progeny.PictureLink = await progenyService.ResizeImage(progeny.PictureLink);
            }

            progeny.CreatedBy = User.GetUserId();

            progeny = await progenyService.AddProgeny(progeny, currentUserInfo);
            
            return Ok(progeny);
        }

        /// <summary>
        /// Updates the Progeny entity with the specified id.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to update.</param>
        /// <param name="value">Progeny object with the updated properties.</param>
        /// <returns>The updated Progeny object.</returns>
        // PUT api/progeny/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Progeny value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Progeny progeny = await progenyService.GetProgeny(id, currentUserInfo);

            if (progeny == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!progeny.IsInAdminList(userEmail))
            {
                return Unauthorized();
            }
            
            progeny.BirthDay = value.BirthDay;
            progeny.Name = value.Name;
            progeny.NickName = value.NickName;
            progeny.TimeZone = value.TimeZone;
            progeny.PictureLink = value.PictureLink;
            progeny.ModifiedBy = User.GetUserId();
            
            progeny = await progenyService.UpdateProgeny(progeny, currentUserInfo);
            if (progeny == null)
            {
                return Unauthorized();
            }

            return Ok(progeny);
        }

        /// <summary>
        /// Deletes the Progeny entity with the specified id.
        /// </summary>
        /// <param name="id">The ProgenyId of the entity to delete.</param>
        /// <returns>NoContentResult.</returns>
        // DELETE api/progeny/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            // Todo: Implement confirmation mail to verify that all content really should be deleted.
            Progeny progeny = await progenyService.GetProgeny(id, currentUserInfo);
            if (progeny == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!progeny.IsInAdminList(userEmail))
            {
                return Unauthorized();
            }
            
            progeny.ModifiedBy = User.GetUserId();

            Progeny deletedProgeny = await progenyService.DeleteProgeny(progeny, currentUserInfo);
            if (deletedProgeny == null)
            {
                return Unauthorized();
            }

            ProgenyInfo progenyInfo = await progenyService.GetProgenyInfo(progeny.Id, currentUserInfo);
            _ = await progenyService.DeleteProgenyInfo(progenyInfo, currentUserInfo);
            
            return NoContent();

        }

        /// <summary>
        /// Gets the ProgenyInfo entity for the specified ProgenyId.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <returns>OkResult with the ProgenyInfo object.</returns>
        [HttpGet("[action]/{progenyId:int}")]
        public async Task<IActionResult> GetProgenyInfo(int progenyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            ProgenyInfo result = await progenyService.GetProgenyInfo(progenyId, currentUserInfo);

            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();
        }

        /// <summary>
        /// Updates a ProgenyInfo entity in the database.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny the ProgenyInfo entity belongs to.</param>
        /// <param name="progenyInfo">The ProgenyInfo object with the updated properties.</param>
        /// <returns>OkResult with the updated ProgenyInfo object.</returns>
        [HttpPut]
        [Route("[action]/{progenyId:int}")]
        public async Task<IActionResult> UpdateProgenyInfo(int progenyId, [FromBody] ProgenyInfo progenyInfo)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
            if (progeny == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!progeny.IsInAdminList(userEmail))
            {
                return Unauthorized();
            }

            if (progenyId != progenyInfo.ProgenyId)
            {
                return BadRequest();
            }
            progenyInfo.ModifiedBy = User.GetUserId();
            ProgenyInfo updatedProgenyInfo = await progenyService.UpdateProgenyInfo(progenyInfo, currentUserInfo);
            if (updatedProgenyInfo == null)
            {
                return Unauthorized();
            }

            return Ok(updatedProgenyInfo);
        }
    }
}
