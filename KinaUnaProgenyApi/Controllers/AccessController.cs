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

        // GET api/access/progeny/[id]
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

        // GET api/access/5
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

        // POST api/access
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserAccess value)
        {
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            if (prog != null)
            {
                // Check if user is allowed to add users for this child.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            
            UserAccess userAccess = new UserAccess();
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;

            // If a UserAccess entry with the same user and progeny exists, replace it.
            List<UserAccess> progenyAccessList = await _userAccessService.GetUsersUserAccessList(userAccess.UserId);// _context.UserAccessDb.Where(u => u.UserId.ToUpper() == userAccess.UserId.ToUpper()).ToListAsync();
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == userAccess.ProgenyId);
            if (oldUserAccess != null)
            {
                await _userAccessService.RemoveUserAccess(oldUserAccess.AccessId, oldUserAccess.ProgenyId, oldUserAccess.UserId);
            }

            
            userAccess = await _userAccessService.AddUserAccess(userAccess);

            Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
            if (userAccess.AccessLevel == (int)AccessLevel.Private && !progeny.IsInAdminList(userAccess.UserId))
            {

                progeny.Admins = progeny.Admins + ", " + userAccess.UserId.ToUpper();
                await _userAccessService.UpdateProgenyAdmins(progeny);
            }

            if (userAccess.AccessLevel == (int) AccessLevel.Private)
            {
                await _userAccessService.SetProgenyUserIsAdminInCache(userAccess.UserId);
            }

            await _userAccessService.SetProgenyUserAccessListInCache(userAccess.ProgenyId);
            await _userAccessService.SetUsersUserAccessListInCache(userAccess.UserId);
            await _userAccessService.SetUserAccessInCache(userAccess.AccessId);

            string title = "User added for " + prog.NickName;
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail()); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added user: " + userAccess.UserId;
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = userAccess.ProgenyId;
                tItem.AccessLevel = 0;
                tItem.ItemId = userAccess.AccessId.ToString();
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(userAccess);
        }

        // PUT api/access/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UserAccess value)
        {
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            if (prog != null)
            {
                // Check if user is allowed to edit user access for this child.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            UserAccess userAccess = await _userAccessService.GetUserAccess(id); // _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);

            if (userAccess == null)
            {
                return NotFound();
            }
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;
            
            userAccess = await _userAccessService.UpdateUserAccess(userAccess);
            Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
            if (userAccess.AccessLevel == (int)AccessLevel.Private && !progeny.IsInAdminList(userAccess.UserId))
            {

                progeny.Admins = progeny.Admins + ", " + userAccess.UserId.ToUpper();
                await _userAccessService.UpdateProgenyAdmins(progeny);
            }

            if (userAccess.AccessLevel == (int)AccessLevel.Private)
            {
                await _userAccessService.SetProgenyUserIsAdminInCache(userAccess.UserId);
            }

            await _userAccessService.SetProgenyUserAccessListInCache(userAccess.ProgenyId);
            await _userAccessService.SetUsersUserAccessListInCache(userAccess.UserId);
            await _userAccessService.SetUserAccessInCache(userAccess.AccessId);

            string title = "User access modified for " + prog.NickName;
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail()); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " modified access for user: " + userAccess.UserId;
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = userAccess.ProgenyId;
                tItem.AccessLevel = 0;
                tItem.ItemId = userAccess.AccessId.ToString();
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(userAccess);
        }

        // DELETE api/access/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            UserAccess userAccess = await _userAccessService.GetUserAccess(id); // _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                // Check if child exists.
                Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == userAccess.ProgenyId);
                if (progeny != null)
                {
                    // Check if user is allowed to delete users for this child.
                    
                    if (!progeny.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                if (userAccess.AccessLevel == (int)AccessLevel.Private && progeny.IsInAdminList(userAccess.UserId))
                {
                    string[] adminList = progeny.Admins.Split(',');
                    progeny.Admins = "";
                    foreach (string adminItem in adminList)
                    {
                        if (!adminItem.Trim().ToUpper().Equals(userAccess.UserId.Trim().ToUpper()))
                        {
                            progeny.Admins = progeny.Admins + ", " + userAccess.UserId.ToUpper();
                        }
                    }
                    progeny.Admins = progeny.Admins.Trim(',');
                    await _userAccessService.UpdateProgenyAdmins(progeny);
                }

                await _userAccessService.RemoveUserAccess(userAccess.AccessId, userAccess.ProgenyId, userAccess.UserId);

                string title = "User removed for " + progeny.NickName;
                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
                if (userinfo != null)
                {
                    string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " removed user: " + userAccess.UserId;
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = userAccess.ProgenyId;
                    tItem.AccessLevel = 0;
                    tItem.ItemId = userAccess.AccessId.ToString();
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.UserAccess;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }
            
            return NotFound();
        }

        // GET api/access/progenylistbyuser/[userid]
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
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _progenyService.GetProgeny(ua.ProgenyId);
                        if (string.IsNullOrEmpty(prog.PictureLink))
                        {
                            prog.PictureLink = Constants.ProfilePictureUrl;
                        }
                        result.Add(prog);
                    }

                    return Ok(result);
                }
            }
            
            return NotFound();
        }

        // GET api/access/progenylistbyusermobile/[userid]
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
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _progenyService.GetProgeny(ua.ProgenyId);
                        if (string.IsNullOrEmpty(prog.PictureLink))
                        {
                            prog.PictureLink = Constants.ProfilePictureUrl;
                        }
                        if (!prog.PictureLink.ToLower().StartsWith("http"))
                        {
                            prog.PictureLink = _imageStore.UriFor(prog.PictureLink, "progeny");
                        }
                        result.Add(prog);
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
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _progenyService.GetProgeny(ua.ProgenyId);
                        if (string.IsNullOrEmpty(prog.PictureLink))
                        {
                            prog.PictureLink = Constants.ProfilePictureUrl;
                        }

                        if (!prog.PictureLink.ToLower().StartsWith("http"))
                        {
                            prog.PictureLink = _imageStore.UriFor(prog.PictureLink, "progeny");
                        }

                        result.Add(prog);
                    }

                    return Ok(result);
                }
            }

            return NotFound();
        }

        // GET api/access/accesslistbyuser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AccessListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        ua.Progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                    }
                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }

        // GET api/access/adminlistbyuser/[useremail]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AdminListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);
                userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                        progenyList.Add(progeny);
                    }

                    if (progenyList.Any())
                    {
                        return Ok(progenyList);
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
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);
                userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                        progenyList.Add(progeny);
                    }

                    if (progenyList.Any())
                    {
                        return Ok(progenyList);
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
                List<UserAccess> userAccessList = await _userAccessService.GetUsersUserAccessList(id);
                userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();
                
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                        progenyList.Add(progeny);
                    }

                    if (progenyList.Any())
                    {
                        return Ok(progenyList);
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
                    foreach (UserAccess ua in userAccessList)
                    {
                        ua.UserId = newEmail;
                        await _userAccessService.UpdateUserAccess(ua);
                    }

                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }
    }
}
