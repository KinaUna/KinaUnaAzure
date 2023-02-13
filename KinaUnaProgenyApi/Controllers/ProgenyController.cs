using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ProgenyController : ControllerBase
    {
        private readonly ImageStore _imageStore;
        private readonly IProgenyService _progenyService;
        private readonly IUserAccessService _userAccessService;

        public ProgenyController(ImageStore imageStore, IProgenyService progenyService, IUserAccessService userAccessService)
        {
            _imageStore = imageStore;
            _progenyService = progenyService;
            _userAccessService = userAccessService;
        }

        // GET api/progeny/parent/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Parent(string id)
        {
            // Check if user should be allowed to access this.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(id); 
                if (progenyList.Any())
                {
                    return Ok(progenyList);
                }
                else
                {
                    return NotFound();
                }
            }

            return Unauthorized();
        }

        // GET api/progeny/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                Progeny result = await _progenyService.GetProgeny(id); 
                
                if (result != null)
                {
                    return Ok(result);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // For Xamarin mobile app.
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Mobile(int id)
        {
            Progeny result = await _progenyService.GetProgeny(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                if (!result.PictureLink.ToLower().StartsWith("http"))
                {
                    result.PictureLink = _imageStore.UriFor(result.PictureLink, "progeny");
                }
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/progeny
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Progeny value)
        {
            Progeny progeny = new Progeny();
            progeny.Name = value.Name;
            progeny.NickName = value.NickName;
            progeny.BirthDay = value.BirthDay;
            progeny.TimeZone = value.TimeZone;
            progeny.Admins = value.Admins;
            if (string.IsNullOrEmpty(value.PictureLink))
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }
            progeny.PictureLink = value.PictureLink;

            progeny = await _progenyService.AddProgeny(progeny);

            if (progeny.Admins.Contains(','))
            {
                List<string> adminList = progeny.Admins.Split(',').ToList();
                foreach (string adminEmail in adminList)
                {
                    UserAccess ua = new UserAccess();
                    ua.AccessLevel = 0;
                    ua.ProgenyId = progeny.Id;
                    ua.UserId = adminEmail.Trim();
                    if (ua.UserId.IsValidEmail())
                    {
                        await _userAccessService.AddUserAccess(ua);
                    }
                }
            }
            else
            {
                UserAccess ua = new UserAccess();
                ua.AccessLevel = 0;
                ua.ProgenyId = progeny.Id;
                ua.UserId = progeny.Admins.Trim();
                if (ua.UserId.IsValidEmail())
                {
                    await _userAccessService.AddUserAccess(ua);
                }

            }

            return Ok(progeny);
        }

        // PUT api/progeny/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Progeny value)
        {
            Progeny progeny = await _progenyService.GetProgeny(id);

            if (progeny == null)
            {
                return NotFound();
            }

            // Check if user is allowed to edit this child.
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
                        UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progeny.Id, email.Trim());
                        if (userAccess.AccessLevel != (int)AccessLevel.Private)
                        {
                            userAccess.AccessLevel = (int)AccessLevel.Private;
                            await _userAccessService.UpdateUserAccess(userAccess);
                        }
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

                        if (!isInNewList)
                        {
                            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progeny.Id, email.Trim());
                            userAccess.AccessLevel = (int) AccessLevel.Family;
                            await _userAccessService.UpdateUserAccess(userAccess);
                        }
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

            progeny = await _progenyService.UpdateProgeny(progeny);
            
            return Ok(progeny);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Todo: Implement confirmation mail to verify that all content really should be deleted.
            Progeny progeny = await _progenyService.GetProgeny(id);
            if (progeny != null)
            {
                // Check if user is allowed to edit this child.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
                
                
                if (!progeny.PictureLink.ToLower().StartsWith("http") && !String.IsNullOrEmpty(progeny.PictureLink))
                {
                    await _imageStore.DeleteImage(progeny.PictureLink, "progeny");
                }

                List<UserAccess> userAccessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id);
                    
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        await _userAccessService.RemoveUserAccess(ua.AccessId, ua.ProgenyId, ua.UserId);
                    }
                }

                await _progenyService.DeleteProgeny(progeny);
                return NoContent();
            }
            else
            {
                return NotFound();
            }
            
        }
    }
}
