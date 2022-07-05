using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly ApplicationDbContext _appDbContext;
        private readonly IDataService _dataService;
        private readonly ImageStore _imageStore;

        public UserInfoController(ProgenyDbContext context, IDataService dataService, ApplicationDbContext appDbContext, ImageStore imageStore)
        {
            _context = context;
            _dataService = dataService;
            _appDbContext = appDbContext;
            _imageStore = imageStore;
        }
        
        // GET api/userinfo/byemail/[useremail]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ByEmail(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _dataService.GetProgenyUserIsAdmin(userEmail); 
                if (progenyList.Any())
                {
                    foreach (Progeny prog in progenyList)
                    {
                        List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(prog.Id);
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userAccess.UserId.ToUpper() == id.ToUpper())
                                {
                                    allowAccess = true;
                                }
                            }
                        }
                    }
                }
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(id); 
            if (allowAccess && userinfo != null && userinfo.Id != 0)
            {
                userinfo.CanUserAddItems = false;
                userinfo.AccessList = await _dataService.GetUsersUserAccessList(userinfo.UserEmail);
                userinfo.ProgenyList = new List<Progeny>();
                if (userinfo.AccessList.Any())
                {
                    foreach (UserAccess ua in userinfo.AccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId);
                        userinfo.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            userinfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                if (userEmail.ToUpper() == id.ToUpper())
                {
                    UserInfo newUserinfo = new UserInfo();
                    newUserinfo.UserEmail = userEmail;
                    newUserinfo.ViewChild = 0;
                    newUserinfo.UserId = User.GetUserId();
                    newUserinfo.Timezone = User.GetUserTimeZone();
                    newUserinfo.UserName = User.GetUserUserName();
                    if (String.IsNullOrEmpty(newUserinfo.UserName))
                    {
                        newUserinfo.UserName = newUserinfo.UserEmail;
                    }

                    _context.UserInfoDb.Add(newUserinfo);
                    await _context.SaveChangesAsync();
                    await _dataService.SetUserInfoByEmail(userEmail);
                    userinfo = await _dataService.GetUserInfoByEmail(id);
                    userinfo.CanUserAddItems = false;
                    userinfo.AccessList = await _dataService.GetUsersUserAccessList(userinfo.UserEmail);
                    userinfo.ProgenyList = new List<Progeny>();
                    if (userinfo.AccessList.Any())
                    {
                        foreach (UserAccess ua in userinfo.AccessList)
                        {
                            Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId);
                            userinfo.ProgenyList.Add(progeny);
                            if (ua.AccessLevel == 0 || ua.CanContribute)
                            {
                                userinfo.CanUserAddItems = true;
                            }
                        }
                    }
                }
                else
                {
                    userinfo = new UserInfo();
                    userinfo.ViewChild = 0;
                    userinfo.UserEmail = "Unknown";
                    userinfo.CanUserAddItems = false;
                    userinfo.UserId = "Unknown";
                    userinfo.AccessList = new List<UserAccess>();
                    userinfo.ProgenyList = new List<Progeny>();
                }
                
            }

            return Ok(userinfo);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> UserInfoByEmail([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _dataService.GetProgenyUserIsAdmin(userEmail); 
                if (progenyList.Any())
                {
                    foreach (Progeny prog in progenyList)
                    {
                        List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(prog.Id);
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userAccess.UserId.ToUpper() == id.ToUpper())
                                {
                                    allowAccess = true;
                                }
                            }
                        }
                    }
                }
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(id); 
            if (allowAccess && userinfo != null && userinfo.Id != 0)
            {
                userinfo.CanUserAddItems = false;
                userinfo.AccessList = await _dataService.GetUsersUserAccessList(userinfo.UserEmail);
                userinfo.ProgenyList = new List<Progeny>();
                if (userinfo.AccessList.Any())
                {
                    foreach (UserAccess ua in userinfo.AccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId);
                        userinfo.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            userinfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                if (userEmail.ToUpper() == id.ToUpper())
                {
                    UserInfo newUserinfo = new UserInfo();
                    newUserinfo.UserEmail = userEmail;
                    newUserinfo.ViewChild = 0;
                    newUserinfo.UserId = User.GetUserId();
                    newUserinfo.Timezone = User.GetUserTimeZone();
                    newUserinfo.UserName = User.GetUserUserName();
                    if (String.IsNullOrEmpty(newUserinfo.UserName))
                    {
                        newUserinfo.UserName = newUserinfo.UserEmail;
                    }

                    _context.UserInfoDb.Add(newUserinfo);
                    await _context.SaveChangesAsync();
                    await _dataService.SetUserInfoByEmail(userEmail);
                    userinfo = await _dataService.GetUserInfoByEmail(id);
                    userinfo.CanUserAddItems = false;
                    userinfo.AccessList = await _dataService.GetUsersUserAccessList(userinfo.UserEmail);
                    userinfo.ProgenyList = new List<Progeny>();
                    if (userinfo.AccessList.Any())
                    {
                        foreach (UserAccess ua in userinfo.AccessList)
                        {
                            Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId);
                            userinfo.ProgenyList.Add(progeny);
                            if (ua.AccessLevel == 0 || ua.CanContribute)
                            {
                                userinfo.CanUserAddItems = true;
                            }
                        }
                    }
                }
                else
                {
                    userinfo = new UserInfo();
                    userinfo.ViewChild = 0;
                    userinfo.UserEmail = "Unknown";
                    userinfo.CanUserAddItems = false;
                    userinfo.UserId = "Unknown";
                    userinfo.AccessList = new List<UserAccess>();
                    userinfo.ProgenyList = new List<Progeny>();
                }
                
            }

            return Ok(userinfo);
        }
        
        // GET api/userinfo/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInfo(int id)
        {
            UserInfo result = await _dataService.GetUserInfoById(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.ToUpper() == result.UserEmail.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _dataService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny prog in progenyList)
                    {
                        List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(prog.Id);
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userAccess.UserId.ToUpper() == result.UserEmail.ToUpper())
                                {
                                    allowAccess = true;
                                }
                            }
                        }
                    }
                }
            }
            
            if (allowAccess)
            {
                result.CanUserAddItems = false;
                result.AccessList = await _dataService.GetUsersUserAccessList(result.UserEmail); 
                result.ProgenyList = new List<Progeny>();
                if (result.AccessList.Any())
                {
                    foreach (UserAccess ua in result.AccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId); 
                        result.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            result.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                result = new UserInfo();
                result.ViewChild = 0;
                result.UserEmail = "Unknown";
                result.CanUserAddItems = false;
                result.UserId = "Unknown";
                result.AccessList = new List<UserAccess>();
                result.ProgenyList = new List<Progeny>();
                
            }
            return Ok(result);
        }

        // GET api/userinfo/id
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ByUserId(string id)
        {
            UserInfo result = await _dataService.GetUserInfoByUserId(id);
            if (result == null)
            {
                result = new UserInfo();
                result.ViewChild = 0;
                result.UserEmail = "Unknown";
                result.CanUserAddItems = false;
                result.UserId = "Unknown";
                result.AccessList = new List<UserAccess>();
                result.ProgenyList = new List<Progeny>();
            }
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            // Todo: do not allow access, unless user is a Pivoq Organizer or has been granted access otherwise.
            bool allowAccess = false;
            if (userEmail.ToUpper() == result.UserEmail?.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _dataService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny prog in progenyList)
                    {
                        List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(prog.Id); 
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (result.UserEmail != null && userAccess.UserId.ToUpper() == result.UserEmail.ToUpper())
                                {
                                    allowAccess = true;
                                }
                            }
                        }
                    }
                }
            }

            if (allowAccess)
            {
                result.CanUserAddItems = false;
                result.AccessList = await _dataService.GetUsersUserAccessList(result.UserEmail);
                result.ProgenyList = new List<Progeny>();
                if (result.AccessList.Any())
                {
                    foreach (UserAccess ua in result.AccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId); 
                        result.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            result.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                result = new UserInfo();
                result.ViewChild = 0;
                result.UserEmail = "Unknown";
                result.CanUserAddItems = false;
                result.UserId = "Unknown";
                result.AccessList = new List<UserAccess>();
                result.ProgenyList = new List<Progeny>();

            }
            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ByUserIdPost([FromBody] string id)
        {
            UserInfo result = await _dataService.GetUserInfoByUserId(id);
            if (result == null)
            {
                result = new UserInfo();
                result.ViewChild = 0;
                result.UserEmail = "Unknown";
                result.CanUserAddItems = false;
                result.UserId = "Unknown";
                result.AccessList = new List<UserAccess>();
                result.ProgenyList = new List<Progeny>();
            }
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            // Todo: do not allow access, unless user is a Pivoq Organizer or has been granted access otherwise.
            bool allowAccess = false;
            if (userEmail.ToUpper() == result.UserEmail?.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _dataService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny prog in progenyList)
                    {
                        List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(prog.Id); 
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (result.UserEmail != null && userAccess.UserId.ToUpper() == result.UserEmail.ToUpper())
                                {
                                    allowAccess = true;
                                }
                            }
                        }
                    }
                }
            }

            if (allowAccess)
            {
                result.CanUserAddItems = false;
                result.AccessList = await _dataService.GetUsersUserAccessList(result.UserEmail);
                result.ProgenyList = new List<Progeny>();
                if (result.AccessList.Any())
                {
                    foreach (UserAccess ua in result.AccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId); 
                        result.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            result.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                result = new UserInfo();
                result.ViewChild = 0;
                result.UserEmail = "Unknown";
                result.CanUserAddItems = false;
                result.UserId = "Unknown";
                result.AccessList = new List<UserAccess>();
                result.ProgenyList = new List<Progeny>();

            }
            return Ok(result);
        }
        
        [HttpGet("[action]/")]
        public async Task<IActionResult> GetAll()
        {
            
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await _dataService.GetUserInfoByEmail(userEmail);

            if (currentUserInfo.IsKinaUnaAdmin || currentUserInfo.IsPivoqAdmin)
            {
                List<UserInfo> result = await _dataService.GetAllUserInfos();

                return Ok(result);
            }

            return Ok(new List<UserInfo>());
        }

        [HttpPost("[action]/")]
        public async Task<IActionResult> CheckCurrentUser()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo userInfo = await _dataService.GetUserInfoByUserId(User.GetUserId());
            if (userInfo.UserEmail.ToUpper() == userEmail.ToUpper() && !userInfo.Deleted)
            {
                return Ok(userInfo);
            }

            return Unauthorized();
        }

        // POST api/userinfo
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserInfo value)
        {
            UserInfo userinfo = new UserInfo();
            userinfo.ViewChild = value?.ViewChild ?? 0;
            userinfo.UserEmail = value?.UserEmail ?? "";
            userinfo.UserId = value?.UserId ?? "";
            userinfo.Timezone = value?.Timezone ?? "Central European Standard Time";
            userinfo.FirstName = value?.FirstName ?? "";
            userinfo.MiddleName = value?.MiddleName ?? "";
            userinfo.LastName = value?.LastName ?? "";
            userinfo.PhoneNumber = value?.PhoneNumber ?? "";
            userinfo.ProfilePicture = value?.ProfilePicture ?? "";
            userinfo.UserName = value?.UserName ?? userinfo.UserEmail;
            userinfo.IsKinaUnaUser = value?.IsKinaUnaUser ?? true;
            userinfo.IsPivoqUser = value?.IsPivoqUser ?? false;
            userinfo.IsKinaUnaAdmin = false;
            userinfo.IsPivoqAdmin = false;
            userinfo.Deleted = false;
            userinfo.DeletedTime = DateTime.UtcNow;
            userinfo.UpdatedTime = DateTime.UtcNow;
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() != userinfo.UserEmail.ToUpper())
            {
                return Unauthorized();
            }

            _context.UserInfoDb.Add(userinfo);
            await _context.SaveChangesAsync();

            userinfo.AccessList = await _context.UserAccessDb.Where(u => u.UserId.ToUpper() == userinfo.UserEmail.ToUpper()).ToListAsync();

            userinfo.ProgenyList = new List<Progeny>();
            if (userinfo.AccessList.Any())
            {
                foreach (UserAccess ua in userinfo.AccessList)
                {
                    Progeny progeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    userinfo.ProgenyList.Add(progeny);
                    if (ua.AccessLevel == 0 || ua.CanContribute)
                    {
                        userinfo.CanUserAddItems = true;
                    }
                }
            }

            await _dataService.SetUserInfoByEmail(userinfo.UserEmail);
            return Ok(userinfo);
        }

        // PUT api/userinfo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] UserInfo value)
        {
            UserInfo userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserId == value.UserId);
            if (userinfo == null)
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserId == id);
            }

            if (userinfo == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo requester = await _dataService.GetUserInfoByEmail(userEmail);
            // Only allow the user themselves to change userinfo.
            bool allowAccess = false;
            if (userEmail.ToUpper() == userinfo.UserEmail.ToUpper())
            {

                allowAccess = true;
            }
            else
            {
                if (requester.IsKinaUnaAdmin || requester.IsPivoqAdmin)
                {
                    allowAccess = true;
                }
            }

            if (!allowAccess)
            {
                return Unauthorized();
            }

            userinfo.FirstName = value.FirstName;
            userinfo.MiddleName = value.MiddleName;
            userinfo.LastName = value.LastName;
            userinfo.UserName = value.UserName;
            userinfo.PhoneNumber = value.PhoneNumber;
            userinfo.ViewChild = value.ViewChild;
            userinfo.IsKinaUnaUser = value.IsKinaUnaUser;
            userinfo.IsPivoqUser = value.IsPivoqUser;
            userinfo.Deleted = value.Deleted;
            if (value.Deleted)
            {
                userinfo.DeletedTime = DateTime.UtcNow;
            }
            userinfo.DeletedTime = value.DeletedTime;
            userinfo.UpdatedTime = DateTime.UtcNow;
            if (!String.IsNullOrEmpty(value.Timezone))
            {
                userinfo.Timezone = value.Timezone;
            }
            if (!String.IsNullOrEmpty(value.ProfilePicture))
            {
                if (!String.IsNullOrEmpty(userinfo.ProfilePicture))
                {
                    string oldPictureLink = userinfo.ProfilePicture;
                    if (!oldPictureLink.ToLower().StartsWith("http") && !String.IsNullOrEmpty(oldPictureLink))
                    {
                        if (oldPictureLink != value.ProfilePicture)
                        {
                            await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Profiles);
                        }
                    }
                }
                
                userinfo.ProfilePicture = value.ProfilePicture;
            }

            if (value.UpdateIsAdmin)
            {
                if (requester.IsKinaUnaAdmin || requester.IsPivoqAdmin)
                {
                    userinfo.IsKinaUnaAdmin = value.IsKinaUnaAdmin;
                    userinfo.IsPivoqAdmin = value.IsPivoqAdmin;
                }
            }
            
            
            _context.UserInfoDb.Update(userinfo);
            await _context.SaveChangesAsync();

            await _dataService.SetUserInfoByEmail(userinfo.UserEmail);

            // Todo: This should be done via api instead of direct database access.
            ApplicationUser user = await _appDbContext.Users.SingleOrDefaultAsync(u => u.Id == userinfo.UserId);
            if (user != null)
            {
                user.FirstName = userinfo.FirstName;
                user.MiddleName = userinfo.MiddleName;
                user.LastName = userinfo.LastName;
                user.UserName = userinfo.UserName;
                user.TimeZone = userinfo.Timezone;

                _appDbContext.Users.Update(user);

                await _appDbContext.SaveChangesAsync();
            }
            
            return Ok(userinfo);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.Id == id);
            
            if (userinfo != null && userinfo.Deleted && userinfo.DeletedTime < (DateTime.UtcNow - TimeSpan.FromDays(30)))
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (userEmail.ToUpper() != userinfo.UserEmail.ToUpper())
                {
                    return Unauthorized();
                }

                await _imageStore.DeleteImage(userinfo.ProfilePicture, BlobContainers.Profiles);

                List<UserAccess> accessList = await _dataService.GetUsersUserAccessList(userinfo.UserEmail);
                foreach (UserAccess access in accessList)
                {
                    await _dataService.RemoveUserAccess(access.AccessId, access.ProgenyId, access.UserId);
                }

                List<MobileNotification> notificationsList = await _context.MobileNotificationsDb.Where(n => n.UserId == userinfo.UserId).ToListAsync();
                _context.MobileNotificationsDb.RemoveRange(notificationsList);
                _context.UserInfoDb.Remove(userinfo);

                await _context.SaveChangesAsync();
                await _dataService.RemoveUserInfoByEmail(userinfo.UserEmail, userinfo.UserId, userinfo.Id);
                
                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/")]
        public async Task<IActionResult> GetDeletedUserInfos()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await _dataService.GetUserInfoByEmail(userEmail);

            if (currentUserInfo.IsKinaUnaAdmin || currentUserInfo.IsPivoqAdmin)
            {
                List<UserInfo> deletedUsersList = await _dataService.GetDeletedUserInfos();
                return Ok(deletedUsersList);
            }

            return Unauthorized();
        }
    }
}