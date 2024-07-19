using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
    public class UserInfoController(
        ApplicationDbContext appDbContext,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IDataService dataService)
        : ControllerBase
    {
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
            if (userEmail.Equals(id, StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Count != 0)
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Count == 0) continue;

                        foreach (UserAccess userAccess in accessList)
                        {
                            if (userAccess.UserId.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                            {
                                allowAccess = true;
                            }
                        }
                    }
                }
            }

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(id);
            if (allowAccess && userInfo != null && userInfo.Id != 0)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = [];
                if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }
            else
            {
                if (userEmail.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                {
                    UserInfo userinfoToAdd = new()
                    {
                        UserEmail = userEmail,
                        ViewChild = 0,
                        UserId = User.GetUserId(),
                        Timezone = Constants.DefaultTimezone,
                        UserName = User.GetUserUserName()
                    };
                    if (string.IsNullOrEmpty(userinfoToAdd.UserName))
                    {
                        userinfoToAdd.UserName = userinfoToAdd.UserEmail;
                    }

                    _ = await userInfoService.AddUserInfo(userinfoToAdd);

                    userInfo = await userInfoService.GetUserInfoByEmail(id);
                    userInfo.CanUserAddItems = false;
                    userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                    userInfo.ProgenyList = [];
                    if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
                else
                {
                    userInfo = new UserInfo
                    {
                        ViewChild = 0,
                        UserEmail = "Unknown",
                        CanUserAddItems = false,
                        UserId = "Unknown",
                        AccessList = [],
                        ProgenyList = []
                    };
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
            if (userEmail.Equals(id, StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Count != 0)
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Count == 0) continue;

                        foreach (UserAccess userAccess in accessList)
                        {
                            if (userAccess.UserId.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                            {
                                allowAccess = true;
                            }
                        }
                    }
                }
            }

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(id);
            if (allowAccess && userInfo != null && userInfo.Id != 0)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = [];
                if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }
            else
            {
                if (userEmail.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                {
                    UserInfo userInfoToAdd = new()
                    {
                        UserEmail = userEmail,
                        ViewChild = 0,
                        UserId = User.GetUserId(),
                        Timezone = Constants.DefaultTimezone,
                        UserName = User.GetUserUserName()
                    };
                    if (string.IsNullOrEmpty(userInfoToAdd.UserName))
                    {
                        userInfoToAdd.UserName = userInfoToAdd.UserEmail;
                    }

                    _ = await userInfoService.AddUserInfo(userInfoToAdd);
                    userInfo = await userInfoService.GetUserInfoByEmail(id);
                    userInfo.CanUserAddItems = false;
                    userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                    userInfo.ProgenyList = [];
                    if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                    foreach (UserAccess userAccess in userInfo.AccessList)
                    {
                        Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                        userInfo.ProgenyList.Add(progeny);
                        if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                        {
                            userInfo.CanUserAddItems = true;
                        }
                    }
                }
                else
                {
                    userInfo = new UserInfo
                    {
                        ViewChild = 0,
                        UserEmail = "Unknown",
                        CanUserAddItems = false,
                        UserId = "Unknown",
                        AccessList = [],
                        ProgenyList = []
                    };
                }

            }

            return Ok(userInfo);
        }

        // GET api/userinfo/id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetInfo(int id)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoById(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = false;
            if (userEmail.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Count != 0)
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Count == 0) continue;

                        foreach (UserAccess userAccess in accessList)
                        {
                            if (userAccess.UserId.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
                            {
                                allowAccess = true;
                            }
                        }
                    }
                }
            }

            if (allowAccess)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = [];
                if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                foreach (UserAccess ua in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(ua.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (ua.AccessLevel == 0 || ua.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }
            else
            {
                userInfo = new UserInfo
                {
                    ViewChild = 0,
                    UserEmail = "Unknown",
                    CanUserAddItems = false,
                    UserId = "Unknown",
                    AccessList = [],
                    ProgenyList = []
                };
            }
            return Ok(userInfo);
        }

        // GET api/userinfo/id
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ByUserId(string id)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoByUserId(id) ?? new UserInfo
            {
                ViewChild = 0,
                UserEmail = "Unknown",
                CanUserAddItems = false,
                UserId = "Unknown",
                AccessList = [],
                ProgenyList = []
            };

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            bool allowAccess = false;
            if (userEmail.Equals(userInfo.UserEmail?.ToUpper(), StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Count != 0)
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Count != 0)
                        {
                            foreach (UserAccess userAccess in accessList)
                            {
                                if (userInfo.UserEmail != null && userAccess.UserId.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
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
                userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = [];
                if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }
            else
            {
                userInfo = new UserInfo
                {
                    ViewChild = 0,
                    UserEmail = "Unknown",
                    CanUserAddItems = false,
                    UserId = "Unknown",
                    AccessList = [],
                    ProgenyList = []
                };
            }
            return Ok(userInfo);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ByUserIdPost([FromBody] string id)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoByUserId(id) ?? new UserInfo
            {
                ViewChild = 0,
                UserEmail = "Unknown",
                CanUserAddItems = false,
                UserId = "Unknown",
                AccessList = [],
                ProgenyList = []
            };
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            bool allowAccess = false;
            if (userEmail.Equals(userInfo.UserEmail?.ToUpper(), StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(userEmail);
                if (progenyList.Count != 0)
                {
                    foreach (Progeny progeny in progenyList)
                    {
                        List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(progeny.Id);
                        if (accessList.Count == 0) continue;

                        foreach (UserAccess userAccess in accessList)
                        {
                            if (userInfo.UserEmail != null && userAccess.UserId.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
                            {
                                allowAccess = true;
                            }
                        }
                    }
                }
            }

            if (allowAccess)
            {
                userInfo.CanUserAddItems = false;
                userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
                userInfo.ProgenyList = [];
                if (userInfo.AccessList.Count == 0) return Ok(userInfo);

                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }
            else
            {
                userInfo = new UserInfo
                {
                    ViewChild = 0,
                    UserEmail = "Unknown",
                    CanUserAddItems = false,
                    UserId = "Unknown",
                    AccessList = [],
                    ProgenyList = []
                };
            }
            return Ok(userInfo);
        }

        [HttpGet("[action]/")]
        public async Task<IActionResult> GetAll()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            if (!currentUserInfo.IsKinaUnaAdmin) return Ok(new List<UserInfo>());

            List<UserInfo> result = await userInfoService.GetAllUserInfos();
            
            return Ok(result);

        }

        [HttpPost("[action]/")]
        public async Task<IActionResult> CheckCurrentUser()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo userInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (userInfo.UserEmail.Equals(userEmail, StringComparison.CurrentCultureIgnoreCase) && !userInfo.Deleted)
            {
                return Ok(userInfo);
            }

            return Unauthorized();
        }

        // POST api/userinfo
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserInfo value)
        {
            UserInfo userInfo = new()
            {
                ViewChild = value?.ViewChild ?? 0,
                UserEmail = value?.UserEmail ?? "",
                UserId = value?.UserId ?? "",
                Timezone = value?.Timezone ?? "Central European Standard Time",
                FirstName = value?.FirstName ?? "",
                MiddleName = value?.MiddleName ?? "",
                LastName = value?.LastName ?? "",
                PhoneNumber = value?.PhoneNumber ?? "",
                ProfilePicture = value?.ProfilePicture ?? ""
            };
            userInfo.UserName = value?.UserName ?? userInfo.UserEmail;
            userInfo.IsKinaUnaAdmin = false;
            userInfo.Deleted = false;
            userInfo.DeletedTime = DateTime.UtcNow;
            userInfo.UpdatedTime = DateTime.UtcNow;
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                return Unauthorized();
            }

            userInfo = await userInfoService.AddUserInfo(userInfo);

            userInfo.AccessList = await userAccessService.GetUsersUserAccessList(userEmail);

            userInfo.ProgenyList = [];
            if (userInfo.AccessList.Count != 0)
            {
                foreach (UserAccess userAccess in userInfo.AccessList)
                {
                    Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                    userInfo.ProgenyList.Add(progeny);
                    if (userAccess.AccessLevel == 0 || userAccess.CanContribute)
                    {
                        userInfo.CanUserAddItems = true;
                    }
                }
            }

            _ = await userInfoService.SetUserInfoByEmail(userInfo.UserEmail);

            return Ok(userInfo);
        }

        // PUT api/userinfo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] UserInfo value)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoByUserId(value.UserId) ?? await userInfoService.GetUserInfoByUserId(id);

            if (userInfo == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo requester = await userInfoService.GetUserInfoByEmail(userEmail);

            // Only allow the user themselves to change their user info.
            bool allowAccess = false;
            if (userEmail.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                allowAccess = true;
            }
            else
            {
                if (requester.IsKinaUnaAdmin)
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
            
            if (value.UpdateIsAdmin)
            {
                if (requester.IsKinaUnaAdmin)
                {
                    userInfo.IsKinaUnaAdmin = value.IsKinaUnaAdmin;
                }
            }

            userInfo = await userInfoService.UpdateUserInfo(userInfo);


            // Todo: This should be done via api instead of direct database access.
            ApplicationUser user = await appDbContext.Users.SingleOrDefaultAsync(u => u.Id == userInfo.UserId);
            if (user == null) return Ok(userInfo);

            user.FirstName = userInfo.FirstName;
            user.MiddleName = userInfo.MiddleName;
            user.LastName = userInfo.LastName;
            user.UserName = userInfo.UserName;
            user.TimeZone = userInfo.Timezone;

            _ = appDbContext.Users.Update(user);

            _ = await appDbContext.SaveChangesAsync();

            return Ok(userInfo);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoById(id);

            if (userInfo == null || !userInfo.Deleted || userInfo.DeletedTime >= (DateTime.UtcNow - TimeSpan.FromDays(30))) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(userInfo.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                return Unauthorized();
            }
            
            List<UserAccess> accessList = await userAccessService.GetUsersUserAccessList(userInfo.UserEmail);
            foreach (UserAccess access in accessList)
            {
                await userAccessService.RemoveUserAccess(access.AccessId, access.ProgenyId, access.UserId);
            }

            List<MobileNotification> notificationsList = await dataService.GetUsersMobileNotifications(userInfo.UserId, "");
            foreach (MobileNotification notification in notificationsList)
            {
                _ = await dataService.DeleteMobileNotification(notification);
            }

            _ = await userInfoService.DeleteUserInfo(userInfo);

            return NoContent();

        }

        [HttpGet("[action]/")]
        public async Task<IActionResult> GetDeletedUserInfos()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            if (!currentUserInfo.IsKinaUnaAdmin) return Unauthorized();

            List<UserInfo> deletedUsersList = await userInfoService.GetDeletedUserInfos();
            return Ok(deletedUsersList);

        }
    }
}