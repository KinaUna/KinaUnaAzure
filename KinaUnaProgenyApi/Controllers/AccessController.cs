using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Contexts;
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
        private readonly IDataService _dataService;
        private readonly ImageStore _imageStore;
        private readonly AzureNotifications _azureNotifications;

        public AccessController(IDataService dataService, ImageStore imageStore, AzureNotifications azureNotifications)
        {
            _dataService = dataService;
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
        }
        
        // GET api/access/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id)
        {
            List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(id);
            
            if (accessList.Any())
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

                bool allowedAccess = false;
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _dataService.GetProgeny(ua.ProgenyId);
                    
                    ua.User = new ApplicationUser();
                    UserInfo userinfo = await _dataService.GetUserInfoByEmail(ua.UserId);
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
            UserAccess result = await _dataService.GetUserAccess(id);
            result.Progeny = await _dataService.GetProgeny(result.ProgenyId);
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
            Progeny prog = await _dataService.GetProgeny(value.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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
            var progenyAccessList = await _dataService.GetUsersUserAccessList(userAccess.UserId);// _context.UserAccessDb.Where(u => u.UserId.ToUpper() == userAccess.UserId.ToUpper()).ToListAsync();
            var oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == userAccess.ProgenyId);
            if (oldUserAccess != null)
            {
                await _dataService.RemoveUserAccess(oldUserAccess.AccessId, oldUserAccess.ProgenyId, oldUserAccess.UserId);
            }

            
            userAccess = await _dataService.AddUserAccess(userAccess);

            Progeny progeny = await _dataService.GetProgeny(userAccess.ProgenyId);
            if (userAccess.AccessLevel == (int)AccessLevel.Private && !progeny.IsInAdminList(userAccess.UserId))
            {

                progeny.Admins = progeny.Admins + ", " + userAccess.UserId.ToUpper();
                await _dataService.UpdateProgenyAdmins(progeny);
            }

            if (userAccess.AccessLevel == (int) AccessLevel.Private)
            {
                await _dataService.SetProgenyUserIsAdmin(userAccess.UserId);
            }

            await _dataService.SetProgenyUserAccessList(userAccess.ProgenyId);
            await _dataService.SetUsersUserAccessList(userAccess.UserId);
            await _dataService.SetUserAccess(userAccess.AccessId);

            string title = "User added for " + prog.NickName;
            UserInfo userinfo = await _dataService.GetUserInfoByEmail(User.GetEmail()); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
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
            Progeny prog = await _dataService.GetProgeny(value.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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

            UserAccess userAccess = await _dataService.GetUserAccess(id); // _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);

            if (userAccess == null)
            {
                return NotFound();
            }
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;
            
            userAccess = await _dataService.UpdateUserAccess(userAccess);
            Progeny progeny = await _dataService.GetProgeny(userAccess.ProgenyId);
            if (userAccess.AccessLevel == (int)AccessLevel.Private && !progeny.IsInAdminList(userAccess.UserId))
            {

                progeny.Admins = progeny.Admins + ", " + userAccess.UserId.ToUpper();
                await _dataService.UpdateProgenyAdmins(progeny);
            }

            if (userAccess.AccessLevel == (int)AccessLevel.Private)
            {
                await _dataService.SetProgenyUserIsAdmin(userAccess.UserId);
            }

            await _dataService.SetProgenyUserAccessList(userAccess.ProgenyId);
            await _dataService.SetUsersUserAccessList(userAccess.UserId);
            await _dataService.SetUserAccess(userAccess.AccessId);

            string title = "User access modified for " + prog.NickName;
            UserInfo userinfo = await _dataService.GetUserInfoByEmail(User.GetEmail()); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
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

            UserAccess userAccess = await _dataService.GetUserAccess(id); // _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                // Check if child exists.
                Progeny progeny = await _dataService.GetProgeny(userAccess.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == userAccess.ProgenyId);
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
                    await _dataService.UpdateProgenyAdmins(progeny);
                }

                await _dataService.RemoveUserAccess(userAccess.AccessId, userAccess.ProgenyId, userAccess.UserId);

                string title = "User removed for " + progeny.NickName;
                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == User.GetEmail().ToUpper());
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
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _dataService.GetProgeny(ua.ProgenyId);
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
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id);

                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _dataService.GetProgeny(ua.ProgenyId);
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
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id);
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        ua.Progeny = await _dataService.GetProgeny(ua.ProgenyId);
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
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id);
                userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId);
                        progenyList.Add(progeny);
                    }

                    if (progenyList.Any())
                    {
                        return Ok(progenyList);
                    }

                    return Ok();
                }
            }

            return Ok();
        }

        [HttpGet("[action]/{oldEmail}/{newEmail}")]
        public async Task<IActionResult> UpdateAccessListEmailChange(string oldEmail, string newEmail)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == oldEmail.ToUpper())
            {
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(oldEmail); // _context.UserAccessDb.Where(u => u.UserId.ToUpper() == oldEmail.ToUpper()).ToListAsync();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        ua.UserId = newEmail;
                        await _dataService.UpdateUserAccess(ua);
                    }

                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }
    }
}
