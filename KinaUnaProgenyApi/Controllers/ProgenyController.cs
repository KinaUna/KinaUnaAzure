using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
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
    public class ProgenyController(IImageStore imageStore, IProgenyService progenyService, IUserAccessService userAccessService) : ControllerBase
    {
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
            if (!string.IsNullOrEmpty(value.PictureLink))
            {
                progeny.PictureLink = value.PictureLink;
            }
            progeny.TimeZone = value.TimeZone;

            if (!progeny.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase))
            {
                progeny.PictureLink = await progenyService.ResizeImage(progeny.PictureLink);
            }

            progeny = await progenyService.UpdateProgeny(progeny);
            
            return Ok(progeny);
        }

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
                
            await imageStore.DeleteImage(progeny.PictureLink, "progeny");

            List<UserAccess> userAccessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);

            if (userAccessList.Count != 0)
            {
                foreach (UserAccess ua in userAccessList)
                {
                    await userAccessService.RemoveUserAccess(ua.AccessId, ua.ProgenyId, ua.UserId);
                }
            }

            await progenyService.DeleteProgeny(progeny);
            return NoContent();

        }
    }
}
