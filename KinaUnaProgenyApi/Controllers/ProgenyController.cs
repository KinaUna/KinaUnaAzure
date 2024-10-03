using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Progeny data.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="progenyService"></param>
    /// <param name="userAccessService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProgenyController(IImageStore imageStore, IProgenyService progenyService, IUserAccessService userAccessService) : ControllerBase
    {
        /// <summary>
        /// Gets a list of all Progeny that the current user is admin for.
        /// Obsolete: this action will show the user's email in the url.
        /// </summary>
        /// <param name="id">The user's email address.</param>
        /// <returns>List of Progeny.</returns>
        // GET api/progeny/parent/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Parent(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return Unauthorized();

            List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(id);
            if (progenyList.Count != 0)
            {
                return Ok(progenyList);
            }

            return NotFound();

        }

        /// <summary>
        /// Gets the Progeny entity with the specified id.
        /// </summary>
        /// <param name="id">The ProgenyId for the Progeny entity to get.</param>
        /// <returns>The Progeny object with the given ProgenyId.</returns>
        // GET api/progeny/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            Progeny result = await progenyService.GetProgeny(id);

            if (result != null)
            {
                return Ok(result);
            }
            return NotFound();

        }

        /// <summary>
        /// Gets the Progeny entity with the specified id.
        /// For mobile clients.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get.</param>
        /// <returns>Progeny object with the given ProgenyId.</returns>
        // For Xamarin mobile app.
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> Mobile(int id)
        {
            Progeny result = await progenyService.GetProgeny(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            result.PictureLink = imageStore.UriFor(result.PictureLink, "progeny");
            return Ok(result);

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

            progeny = await progenyService.AddProgeny(progeny);

            if (progeny.Admins.Contains(','))
            {
                List<string> adminList = [.. progeny.Admins.Split(',')];
                foreach (string adminEmail in adminList)
                {
                    UserAccess ua = new()
                    {
                        AccessLevel = 0,
                        ProgenyId = progeny.Id,
                        UserId = adminEmail.Trim()
                    };
                    if (ua.UserId.IsValidEmail())
                    {
                        await userAccessService.AddUserAccess(ua);
                    }
                }
            }
            else
            {
                UserAccess ua = new()
                {
                    AccessLevel = 0,
                    ProgenyId = progeny.Id,
                    UserId = progeny.Admins.Trim()
                };

                if (ua.UserId.IsValidEmail())
                {
                    await userAccessService.AddUserAccess(ua);
                }

            }

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
            Progeny progeny = await progenyService.GetProgeny(id);

            if (progeny == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!progeny.IsInAdminList(userEmail))
            {
                return Unauthorized();
            }

            if (!progeny.Admins.ToUpper().Equals(value.Admins.ToUpper()))
            {
                string[] admins = value.Admins.Split(',');
                string[] oldAdmins = progeny.Admins.Split(',');
                bool validAdminEmails = true;
                foreach (string str in admins)
                {
                    if (!str.Trim().IsValidEmail())
                    {
                        validAdminEmails = false;
                    }
                }

                if (validAdminEmails)
                {
                    progeny.Admins = value.Admins;

                    foreach (string email in admins)
                    {
                        UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progeny.Id, email.Trim());
                        if (userAccess.AccessLevel == (int)AccessLevel.Private) continue;

                        userAccess.AccessLevel = (int)AccessLevel.Private;
                        await userAccessService.UpdateUserAccess(userAccess);
                    }

                    foreach (string email in oldAdmins)
                    {
                        bool isInNewList = false;
                        foreach (string newEmail in admins)
                        {
                            if (email.Trim().ToUpper().Equals(newEmail.Trim().ToUpper()))
                            {
                                isInNewList = true;
                            }
                        }

                        if (isInNewList) continue;

                        UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progeny.Id, email.Trim());
                        userAccess.AccessLevel = (int)AccessLevel.Family;
                        await userAccessService.UpdateUserAccess(userAccess);
                    }
                }
            }

            progeny.BirthDay = value.BirthDay;
            progeny.Name = value.Name;
            progeny.NickName = value.NickName;
            progeny.TimeZone = value.TimeZone;
            progeny.PictureLink = value.PictureLink;

            progeny = await progenyService.UpdateProgeny(progeny);
            
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
            // Todo: Implement confirmation mail to verify that all content really should be deleted.
            Progeny progeny = await progenyService.GetProgeny(id);
            if (progeny == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!progeny.IsInAdminList(userEmail))
            {
                return Unauthorized();
            }
            
            CustomResult<List<UserAccess>> userAccessListResult = await userAccessService.GetProgenyUserAccessList(progeny.Id, userEmail);
            if (!userAccessListResult.IsSuccess) return userAccessListResult.ToActionResult();

            foreach (UserAccess ua in userAccessListResult.Value)
            {
                await userAccessService.RemoveUserAccess(ua.AccessId, ua.ProgenyId, ua.UserId);
            }

            await progenyService.DeleteProgeny(progeny);


            return NoContent();

        }
    }
}
