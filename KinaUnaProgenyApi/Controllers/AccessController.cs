using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController : ControllerBase
    {
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ImageStore _imageStore;
        private readonly AzureNotifications _azureNotifications;

        public AccessController(ImageStore imageStore, AzureNotifications azureNotifications, IProgenyService progenyService, IUserInfoService userInfoService, IUserAccessService userAccessService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
        }

        // GET api/Access/Progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id)
        {
            List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(id);
            
            if (accessList.Any())
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

                bool allowedAccess = false;
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                    
                    ua.User = new ApplicationUser();
                    UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(ua.UserId);
                    if (userinfo != null)
                    {
                        ua.User.FirstName = userinfo.FirstName;
                        ua.User.MiddleName = userinfo.MiddleName;
                        ua.User.LastName = userinfo.LastName;
                        ua.User.UserName = userinfo.UserName;
                    }
                    ua.User.Email = ua.UserId;
                    if (ua.User.Email.ToUpper() == userEmail.ToUpper())
                    {
                        allowedAccess = true;
                    }
                }

                if (!allowedAccess)
                {
                    return Unauthorized();
                }

                return Ok(accessList);
            }

            return NotFound();
        }

        // GET api/Access/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccess(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess result = await _userAccessService.GetUserAccess(id);
            result.Progeny = await _progenyService.GetProgeny(result.ProgenyId);
            
            if (result.Progeny.IsInAdminList(User.GetEmail()) || result.UserId.ToUpper() == userEmail.ToUpper())
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/Access
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserAccess value)
        {
            value.Progeny = await _progenyService.GetProgeny(value.ProgenyId);
            if (value.Progeny != null)
            {
                if (!value.Progeny.IsInAdminList(User.GetEmail()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            UserAccess userAccess = await _userAccessService.AddUserAccess(value);
            
            TimeLineItem tItem = new TimeLineItem();
            tItem.CopyUserAccessItemPropertiesForAdd(userAccess);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "User added for " + userAccess.Progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added user: " + userAccess.UserId;
            
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, tItem, userInfo.ProfilePicture);

            return Ok(userAccess);
        }

        // PUT api/Access/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UserAccess value)
        {
            value.Progeny = await _progenyService.GetProgeny(value.ProgenyId);
            if (value.Progeny != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!value.Progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            UserAccess userAccess = await _userAccessService.UpdateUserAccess(value);

            if (userAccess == null)
            {
                return NotFound();
            }
            
            string notificationTitle = "User access modified for " + userAccess.Progeny.NickName;
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
            if (userInfo != null)
            {
                string notificationMessage = userInfo.FullName() + " modified access for user: " + userAccess.UserId;
                
                TimeLineItem timeLineItem = new TimeLineItem();
                timeLineItem.CopyUserAccessItemPropertiesForUpdate(userAccess);

                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            }

            return Ok(userAccess);
        }

        // DELETE api/Access/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            UserAccess userAccess = await _userAccessService.GetUserAccess(id);
            if (userAccess != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                
                userAccess.Progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                if (userAccess.Progeny != null)
                {
                    if (!userAccess.Progeny.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }
                
                await _userAccessService.RemoveUserAccess(userAccess.AccessId, userAccess.ProgenyId, userAccess.UserId);

                string notificationTitle = "User removed for " + userAccess.Progeny.NickName;
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                if (userInfo != null)
                {
                    string notificationMessage = userInfo.FullName() + " removed user: " + userAccess.UserId;
                    TimeLineItem timeLineItem = new TimeLineItem();
                    timeLineItem.CopyUserAccessItemPropertiesForUpdate(userAccess);
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                }

                return NoContent();
            }
            
            return NotFound();
        }

        // GET api/Access/ProgenyListByUser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<Progeny> result = new List<Progeny>();
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        if (string.IsNullOrEmpty(progeny.PictureLink))
                        {
                            progeny.PictureLink = Constants.ProfilePictureUrl;
                        }
                        result.Add(progeny);
                    }

                    return Ok(result);
                }
            }
            
            return NotFound();
        }

        // GET api/Access/ProgenyListByUserMobile/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUserMobile(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<Progeny> result = new List<Progeny>();
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        if (string.IsNullOrEmpty(progeny.PictureLink))
                        {
                            progeny.PictureLink = Constants.ProfilePictureUrl;
                        }
                        if (!progeny.PictureLink.ToLower().StartsWith("http"))
                        {
                            progeny.PictureLink = _imageStore.UriFor(progeny.PictureLink, "progeny");
                        }
                        result.Add(progeny);
                    }

                    return Ok(result);
                }
            }

            return NotFound();
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ProgenyListByUserPostMobile([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<Progeny> result = new List<Progeny>();
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        if (string.IsNullOrEmpty(progeny.PictureLink))
                        {
                            progeny.PictureLink = Constants.ProfilePictureUrl;
                        }

                        if (!progeny.PictureLink.ToLower().StartsWith("http"))
                        {
                            progeny.PictureLink = _imageStore.UriFor(progeny.PictureLink, "progeny");
                        }

                        result.Add(progeny);
                    }

                    return Ok(result);
                }
            }

            return NotFound();
        }

        // GET api/Access/AccessListByUser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AccessListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);
                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        userAccess.Progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                    }
                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }

        // GET api/Access/AdminListByUser/[UserEmail]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AdminListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAdminAccessList(id);
                
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        progenyList.Add(progeny);
                    }

                }
                return Ok(progenyList);
            }

            return Ok();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AdminListByUserPost([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAdminAccessList(id);
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        progenyList.Add(progeny);
                    }

                }

                return Ok(progenyList);
            }

            return Ok();
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> AdminListByUserPivoq([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<Progeny> progenyList = new List<Progeny>();
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAdminAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        progenyList.Add(progeny);
                    }
                }
            }

            return Ok(progenyList);
        }

        [HttpGet("[action]/{oldEmail}/{newEmail}")]
        public async Task<IActionResult> UpdateAccessListEmailChange(string oldEmail, string newEmail)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == oldEmail.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(oldEmail); // _context.UserAccessDb.Where(u => u.UserId.ToUpper() == oldEmail.ToUpper()).ToListAsync();
                if (userAccessList.Any())
                {
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        userAccess.UserId = newEmail;
                        await _userAccessService.UpdateUserAccess(userAccess);
                    }

                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }
    }
}
