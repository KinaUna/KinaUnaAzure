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
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for UserInfo.
    /// </summary>
    /// <param name="appDbContext"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="notificationsService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController(
        ApplicationDbContext appDbContext,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        INotificationsService notificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Get UserInfo by email.
        /// Checks if the user should be allowed access to the UserInfo object.
        /// </summary>
        /// <param name="id">The email address of the user.</param>
        /// <returns>UserInfo</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> UserInfoByEmail([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = await CanCurrentUserAccessUserInfo(userEmail, id);
            
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

        private async Task<bool> CanCurrentUserAccessUserInfo(string currentUserEmail, string otherUserEmail)
        {
            if (currentUserEmail.Equals(otherUserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                // User is trying to access their own UserInfo.
                return true;
            }

            List<Progeny> progenyList = await userAccessService.GetProgenyUserIsAdmin(currentUserEmail);
            if (progenyList.Count == 0) return false;

            foreach (Progeny progeny in progenyList)
            {
                CustomResult<List<UserAccess>> accessListResult = await userAccessService.GetProgenyUserAccessList(progeny.Id, currentUserEmail);
                if (accessListResult.IsFailure) continue;

                foreach (UserAccess userAccess in accessListResult.Value)
                {
                    if (userAccess.UserId.Equals(otherUserEmail, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get UserInfo by UserInfo.Id.
        /// Checks if the user should be allowed access to the UserInfo object.
        /// </summary>
        /// <param name="id">The Id of the UserInfo entity.</param>
        /// <returns>UserInfo object for the user with the given UserId.</returns>
        // GET api/userinfo/id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetInfo(int id)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoById(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = await CanCurrentUserAccessUserInfo(userEmail, userInfo.UserEmail);
            
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

        /// <summary>
        /// Gets a UserInfo object by UserId.
        /// Obsolete, the UserId is leaked in the URL and will show up in logs, use the ByUserIdPost method instead.
        /// Checks if the user should be allowed access to the UserInfo object.
        /// The UserId is the user's id generated by IDP/IdentityServer.
        /// </summary>
        /// <param name="id">The user's UserId.</param>
        /// <returns>UserInfo object for the user.</returns>
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

            bool allowAccess = await CanCurrentUserAccessUserInfo(userEmail, userInfo.UserEmail);
            
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

        /// <summary>
        /// Gets a UserInfo object by UserId.
        /// Checks if the user should be allowed access to the UserInfo object.
        /// The UserId is the user's id generated by IDP/IdentityServer.
        /// </summary>
        /// <param name="id">The user's UserId.</param>
        /// <returns>UserInfo object for the user.</returns>
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

            bool allowAccess = await CanCurrentUserAccessUserInfo(userEmail, userInfo.UserEmail);

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

        /// <summary>
        /// Gets a list of all UserInfo objects.
        /// Only KinaUnaAdmins are allowed to get all UserInfo objects.
        /// </summary>
        /// <returns>List of all UserInfo entities.</returns>
        [HttpGet("[action]/")]
        public async Task<IActionResult> GetAll()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            if (!currentUserInfo.IsKinaUnaAdmin) return Ok(new List<UserInfo>());

            List<UserInfo> result = await userInfoService.GetAllUserInfos();
            
            return Ok(result);

        }

        /// <summary>
        /// Checks if the current user's account is active.
        /// </summary>
        /// <returns>If the account hasn't been deleted returns the UserInfo entity, else Unauthorized.</returns>
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

        /// <summary>
        /// Adds a new UserInfo entity.
        /// </summary>
        /// <param name="value">The UserInfo object to add.</param>
        /// <returns>The added UserInfo object.</returns>
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

        /// <summary>
        /// Updates a UserInfo entity.
        /// Only the user themselves or KinaUnaAdmins are allowed to update UserInfo entities.
        /// </summary>
        /// <param name="id">The UserInfo.Id for the UserInfo to update.</param>
        /// <param name="value">UserInfo object with the updated properties.</param>
        /// <returns>The updated UserInfo object.</returns>
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

        /// <summary>
        /// Deletes a UserInfo entity.
        /// Also removes the user entries from the UserAccess table and MobileNotification table.
        /// This is a hard delete, to soft-delete update the UserInfo with UserInfo.Deleted = true.
        /// </summary>
        /// <param name="id">The UserInfo.Id</param>
        /// <returns>NoContentResult</returns>
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

            List<MobileNotification> notificationsList = await notificationsService.GetUsersMobileNotifications(userInfo.UserId, "");
            foreach (MobileNotification notification in notificationsList)
            {
                _ = await notificationsService.DeleteMobileNotification(notification);
            }

            _ = await userInfoService.DeleteUserInfo(userInfo);

            return NoContent();

        }

        /// <summary>
        /// Gets a list of all (soft) deleted UserInfo entities.
        /// Only KinaUnaAdmins are allowed to get all deleted UserInfo entities.
        /// </summary>
        /// <returns>List of UserInfo</returns>
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