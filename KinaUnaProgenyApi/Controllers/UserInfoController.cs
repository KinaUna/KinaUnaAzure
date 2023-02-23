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
        private readonly ApplicationDbContext _appDbContext;
        private readonly IUserInfoService _userInfoService;
        private readonly IProgenyService _progenyService;
        private readonly IUserAccessService _userAccessService;
        private readonly IDataService _dataService;
        private readonly IImageStore _imageStore;

        public UserInfoController(ApplicationDbContext appDbContext, IImageStore imageStore, IProgenyService progenyService, IUserInfoService userInfoService, IUserAccessService userAccessService, IDataService dataService)
        {
            _appDbContext = appDbContext;
            _imageStore = imageStore;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _dataService = dataService;
        }

        // GET api/userinfo/byemail/[useremail]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ByEmail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = User.GetEmail();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(userEmail); 
                if (progenyList.Any())
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id);
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

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(id); 
            if (allowAccess && userInfo != null && userInfo.Id != 0)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = new List<Progeny>();
                if (userInfo.AccessList.Any())
                {
                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                if (userEmail.ToUpper() == id.ToUpper())
                {
                    UserInfo userinfoToAdd = new UserInfo();
                    userinfoToAdd.UserEmail = userEmail;
                    userinfoToAdd.ViewChild = 0;
                    userinfoToAdd.UserId = User.GetUserId();
                    userinfoToAdd.Timezone = Constants.DefaultTimezone;
                    userinfoToAdd.UserName = User.GetUserUserName();
                    if (String.IsNullOrEmpty(userinfoToAdd.UserName))
                    {
                        userinfoToAdd.UserName = userinfoToAdd.UserEmail;
                    }

                    _ = await _userInfoService.AddUserInfo(userinfoToAdd);
                    
                    userInfo = await _userInfoService.GetUserInfoByEmail(id);
                    userInfo.CanUserAddItems = false;
                    userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                    userInfo.ProgenyList = new List<Progeny>();
                    if (userInfo.AccessList.Any())
                    {
                        foreach (UserAccess userAccess in userInfo.AccessList)
                        {
                            Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                            userInfo.ProgenyList.Add(progeny);
                            if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                            {
                                userInfo.CanUserAddItems = true;
                            }
                        }
                    }
                }
                else
                {
                    userInfo = new UserInfo();
                    userInfo.ViewChild = 0;
                    userInfo.UserEmail = "Unknown";
                    userInfo.CanUserAddItems = false;
                    userInfo.UserId = "Unknown";
                    userInfo.AccessList = new List<UserAccess>();
                    userInfo.ProgenyList = new List<Progeny>();
                }
                
            }

            return Ok(userInfo);
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
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(userEmail); 
                if (progenyList.Any())
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id);
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

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(id); 
            if (allowAccess && userInfo != null && userInfo.Id != 0)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = new List<Progeny>();
                if (userInfo.AccessList.Any())
                {
                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                if (userEmail.ToUpper() == id.ToUpper())
                {
                    UserInfo userInfoToAdd = new UserInfo();
                    userInfoToAdd.UserEmail = userEmail;
                    userInfoToAdd.ViewChild = 0;
                    userInfoToAdd.UserId = User.GetUserId();
                    userInfoToAdd.Timezone = Constants.DefaultTimezone;
                    userInfoToAdd.UserName = User.GetUserUserName();
                    if (String.IsNullOrEmpty(userInfoToAdd.UserName))
                    {
                        userInfoToAdd.UserName = userInfoToAdd.UserEmail;
                    }

                    _ = await _userInfoService.AddUserInfo(userInfoToAdd);
                    userInfo = await _userInfoService.GetUserInfoByEmail(id);
                    userInfo.CanUserAddItems = false;
                    userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                    userInfo.ProgenyList = new List<Progeny>();
                    if (userInfo.AccessList.Any())
                    {
                        foreach (UserAccess userAccess in userInfo.AccessList)
                        {
                            Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                            userInfo.ProgenyList.Add(progeny);
                            if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                            {
                                userInfo.CanUserAddItems = true;
                            }
                        }
                    }
                }
                else
                {
                    userInfo = new UserInfo();
                    userInfo.ViewChild = 0;
                    userInfo.UserEmail = "Unknown";
                    userInfo.CanUserAddItems = false;
                    userInfo.UserId = "Unknown";
                    userInfo.AccessList = new List<UserAccess>();
                    userInfo.ProgenyList = new List<Progeny>();
                }
                
            }

            return Ok(userInfo);
        }
        
        // GET api/userinfo/id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInfo(int id)
        {
            UserInfo userInfo = await _userInfoService.GetUserInfoById(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.ToUpper() == userInfo.UserEmail.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userAccess.UserId.ToUpper() == userInfo.UserEmail.ToUpper())
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
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail); 
                userInfo.ProgenyList = new List<Progeny>();
                if (userInfo.AccessList.Any())
                {
                    foreach (UserAccess ua in userInfo.AccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(ua.ProgenyId); 
                        userInfo.ProgenyList.Add(progeny);
                        if (ua.AccessLevel == 0 || ua.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                userInfo = new UserInfo();
                userInfo.ViewChild = 0;
                userInfo.UserEmail = "Unknown";
                userInfo.CanUserAddItems = false;
                userInfo.UserId = "Unknown";
                userInfo.AccessList = new List<UserAccess>();
                userInfo.ProgenyList = new List<Progeny>();
                
            }
            return Ok(userInfo);
        }

        // GET api/userinfo/id
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ByUserId(string id)
        {
            UserInfo userInfo = await _userInfoService.GetUserInfoByUserId(id);
            if (userInfo == null)
            {
                userInfo = new UserInfo();
                userInfo.ViewChild = 0;
                userInfo.UserEmail = "Unknown";
                userInfo.CanUserAddItems = false;
                userInfo.UserId = "Unknown";
                userInfo.AccessList = new List<UserAccess>();
                userInfo.ProgenyList = new List<Progeny>();
            }
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            // Todo: do not allow access, unless user is a Pivoq Organizer or has been granted access otherwise.
            bool allowAccess = false;
            if (userEmail.ToUpper() == userInfo.UserEmail?.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id); 
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userInfo.UserEmail != null && userAccess.UserId.ToUpper() == userInfo.UserEmail.ToUpper())
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
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = new List<Progeny>();
                if (userInfo.AccessList.Any())
                {
                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId); 
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                userInfo = new UserInfo();
                userInfo.ViewChild = 0;
                userInfo.UserEmail = "Unknown";
                userInfo.CanUserAddItems = false;
                userInfo.UserId = "Unknown";
                userInfo.AccessList = new List<UserAccess>();
                userInfo.ProgenyList = new List<Progeny>();

            }
            return Ok(userInfo);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ByUserIdPost([FromBody] string id)
        {
            UserInfo userInfo = await _userInfoService.GetUserInfoByUserId(id);
            if (userInfo == null)
            {
                userInfo = new UserInfo();
                userInfo.ViewChild = 0;
                userInfo.UserEmail = "Unknown";
                userInfo.CanUserAddItems = false;
                userInfo.UserId = "Unknown";
                userInfo.AccessList = new List<UserAccess>();
                userInfo.ProgenyList = new List<Progeny>();
            }
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            // Todo: do not allow access, unless user is a Pivoq Organizer or has been granted access otherwise.
            bool allowAccess = false;
            if (userEmail.ToUpper() == userInfo.UserEmail?.ToUpper())
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await _userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Any())
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(progeny.Id); 
                        if (accessList.Any())
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userInfo.UserEmail != null && userAccess.UserId.ToUpper() == userInfo.UserEmail.ToUpper())
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
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = new List<Progeny>();
                if (userInfo.AccessList.Any())
                {
                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId); 
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
            }
            else
            {
                userInfo = new UserInfo();
                userInfo.ViewChild = 0;
                userInfo.UserEmail = "Unknown";
                userInfo.CanUserAddItems = false;
                userInfo.UserId = "Unknown";
                userInfo.AccessList = new List<UserAccess>();
                userInfo.ProgenyList = new List<Progeny>();

            }
            return Ok(userInfo);
        }
        
        [HttpGet("[action]/")]
        public async Task<IActionResult> GetAll()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

            if (currentUserInfo.IsKinaUnaAdmin || currentUserInfo.IsPivoqAdmin)
            {
                List<UserInfo> result = await _userInfoService.GetAllUserInfos();

                return Ok(result);
            }

            return Ok(new List<UserInfo>());
        }

        [HttpPost("[action]/")]
        public async Task<IActionResult> CheckCurrentUser()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo userInfo = await _userInfoService.GetUserInfoByUserId(User.GetUserId());
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
            UserInfo userInfo = new UserInfo();
            userInfo.ViewChild = value?.ViewChild ?? 0;
            userInfo.UserEmail = value?.UserEmail ?? "";
            userInfo.UserId = value?.UserId ?? "";
            userInfo.Timezone = value?.Timezone ?? "Central European Standard Time";
            userInfo.FirstName = value?.FirstName ?? "";
            userInfo.MiddleName = value?.MiddleName ?? "";
            userInfo.LastName = value?.LastName ?? "";
            userInfo.PhoneNumber = value?.PhoneNumber ?? "";
            userInfo.ProfilePicture = value?.ProfilePicture ?? "";
            userInfo.UserName = value?.UserName ?? userInfo.UserEmail;
            userInfo.IsKinaUnaUser = value?.IsKinaUnaUser ?? true;
            userInfo.IsPivoqUser = value?.IsPivoqUser ?? false;
            userInfo.IsKinaUnaAdmin = false;
            userInfo.IsPivoqAdmin = false;
            userInfo.Deleted = false;
            userInfo.DeletedTime = DateTime.UtcNow;
            userInfo.UpdatedTime = DateTime.UtcNow;
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() != userInfo.UserEmail.ToUpper())
            {
                return Unauthorized();
            }

            userInfo = await _userInfoService.AddUserInfo(userInfo);

            userInfo.AccessList = await _userAccessService.GetUsersUserAccessList(userEmail);

            userInfo.ProgenyList = new List<Progeny>();
            if (userInfo.AccessList.Any())
            {
                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await _progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }

            _ = await _userInfoService.SetUserInfoByEmail(userInfo.UserEmail);

            return Ok(userInfo);
        }

        // PUT api/userinfo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] UserInfo value)
        {
            UserInfo userInfo = await _userInfoService.GetUserInfoByUserId(value.UserId);
            if (userInfo == null)
            {
                userInfo = await _userInfoService.GetUserInfoByUserId(id);
            }

            if (userInfo == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo requester = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            // Only allow the user themselves to change their user info.
            bool allowAccess = false;
            if (userEmail.ToUpper() == userInfo.UserEmail.ToUpper())
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

            userInfo.FirstName = value.FirstName;
            userInfo.MiddleName = value.MiddleName;
            userInfo.LastName = value.LastName;
            userInfo.UserName = value.UserName;
            userInfo.PhoneNumber = value.PhoneNumber;
            userInfo.ViewChild = value.ViewChild;
            userInfo.IsKinaUnaUser = value.IsKinaUnaUser;
            userInfo.IsPivoqUser = value.IsPivoqUser;
            userInfo.Deleted = value.Deleted;
            
            if (value.Deleted)
            {
                userInfo.DeletedTime = DateTime.UtcNow;
            }
            
            userInfo.DeletedTime = value.DeletedTime;
            userInfo.UpdatedTime = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(value.Timezone))
            {
                userInfo.Timezone = value.Timezone;
            }
            
            if (!string.IsNullOrEmpty(value.ProfilePicture))
            {
                if (!string.IsNullOrEmpty(userInfo.ProfilePicture))
                {
                    string oldPictureLink = userInfo.ProfilePicture;
                    if (!oldPictureLink.ToLower().StartsWith("http") && !string.IsNullOrEmpty(oldPictureLink))
                    {
                        if (oldPictureLink != value.ProfilePicture)
                        {
                            await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Profiles);
                        }
                    }
                }
                
                userInfo.ProfilePicture = value.ProfilePicture;
            }

            if (value.UpdateIsAdmin)
            {
                if (requester.IsKinaUnaAdmin || requester.IsPivoqAdmin)
                {
                    userInfo.IsKinaUnaAdmin = value.IsKinaUnaAdmin;
                    userInfo.IsPivoqAdmin = value.IsPivoqAdmin;
                }
            }

            userInfo = await _userInfoService.UpdateUserInfo(userInfo);
            

            // Todo: This should be done via api instead of direct database access.
            ApplicationUser user = await _appDbContext.Users.SingleOrDefaultAsync(u => u.Id == userInfo.UserId);
            if (user != null)
            {
                user.FirstName = userInfo.FirstName;
                user.MiddleName = userInfo.MiddleName;
                user.LastName = userInfo.LastName;
                user.UserName = userInfo.UserName;
                user.TimeZone = userInfo.Timezone;

                _ = _appDbContext.Users.Update(user);

                _ = await _appDbContext.SaveChangesAsync();
            }
            
            return Ok(userInfo);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo userInfo = await _userInfoService.GetUserInfoById(id);
            
            if (userInfo != null && userInfo.Deleted && userInfo.DeletedTime < (DateTime.UtcNow - TimeSpan.FromDays(30)))
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (userEmail.ToUpper() != userInfo.UserEmail.ToUpper())
                {
                    return Unauthorized();
                }

                _ = await _imageStore.DeleteImage(userInfo.ProfilePicture, BlobContainers.Profiles);

                List<UserAccess> accessList = await _userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                foreach (UserAccess access in accessList)
                {
                    await _userAccessService.RemoveUserAccess(access.AccessId, access.ProgenyId, access.UserId);
                }

                List<MobileNotification> notificationsList = await _dataService.GetUsersMobileNotifications(userInfo.UserId, "");
                foreach (MobileNotification notification in notificationsList)
                {
                    _ = await _dataService.DeleteMobileNotification(notification);
                }

                _ = await _userInfoService.DeleteUserInfo(userInfo);
                
                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/")]
        public async Task<IActionResult> GetDeletedUserInfos()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

            if (currentUserInfo.IsKinaUnaAdmin || currentUserInfo.IsPivoqAdmin)
            {
                List<UserInfo> deletedUsersList = await _userInfoService.GetDeletedUserInfos();
                return Ok(deletedUsersList);
            }

            return Unauthorized();
        }
    }
}