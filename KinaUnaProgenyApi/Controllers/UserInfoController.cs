using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for UserInfo.
    /// </summary>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoController(
        ApplicationDbContext appDbContext,
        IProgenyService progenyService,
        IFamiliesService familiesService,
        IUserInfoService userInfoService,
        IAccessManagementService accessManagementService,
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            bool allowAccess = await CanCurrentUserAccessUserInfo(currentUserInfo.UserEmail, id);
            
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(id);
            if (allowAccess && userInfo != null && userInfo.Id != 0)
            {
                userInfo.CanUserAddItems = false;
                userInfo.ProgenyList = [];
                userInfo.FamilyList = [];
                List<int> progeniesWithAddPermission = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (progeniesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int progenyId in progeniesWithAddPermission)
                    {
                        Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                        if (progeny != null && progeny.Id != 0)
                        {
                            userInfo.ProgenyList.Add(progeny);
                        }
                    }
                }

                List<int> familiesWithAddPermission = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (familiesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int familyId in familiesWithAddPermission)
                    {
                        Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                        if (family != null && family.FamilyId != 0)
                        {
                            userInfo.FamilyList.Add(family);
                        }
                    }
                }
            }
            else
            {
                if (currentUserInfo.UserEmail.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                {
                    UserInfo userInfoToAdd = new()
                    {
                        UserEmail = currentUserInfo.UserEmail,
                        ViewChild = 0,
                        UserId = User.GetUserId(),
                        Timezone = Constants.DefaultTimezone,
                        UserName = User.GetUserUserName()
                    };
                    if (string.IsNullOrEmpty(userInfoToAdd.UserName))
                    {
                        userInfoToAdd.UserName = userInfoToAdd.UserEmail;
                    }

                    userInfo = await userInfoService.AddUserInfo(userInfoToAdd);
                    
                    userInfo.CanUserAddItems = false;
                    userInfo.ProgenyList = [];
                    userInfo.FamilyList = [];
                    List<int> progeniesWithAddPermission = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                    if (progeniesWithAddPermission.Count > 0)
                    {
                        userInfo.CanUserAddItems = true;
                        foreach (int progenyId in progeniesWithAddPermission)
                        {
                            Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                            if (progeny != null && progeny.Id != 0)
                            {
                                userInfo.ProgenyList.Add(progeny);
                            }
                        }
                    }

                    List<int> familiesWithAddPermission = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                    if (familiesWithAddPermission.Count > 0)
                    {
                        userInfo.CanUserAddItems = true;
                        foreach (int familyId in familiesWithAddPermission)
                        {
                            Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                            if (family != null && family.FamilyId != 0)
                            {
                                userInfo.FamilyList.Add(family);
                            }
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
            
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(currentUserEmail);
            if (currentUserInfo == null || currentUserInfo.Id == 0) return false;

            UserInfo otherUserInfo = await userInfoService.GetUserInfoByEmail(otherUserEmail);
            if (otherUserInfo == null || otherUserInfo.Id == 0) return false;

            List<int> familiesCurrentUserCanAccess = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, PermissionLevel.View);
            List<int> familiesOtherUserCanAccess = await accessManagementService.FamiliesUserCanAccess(otherUserInfo, PermissionLevel.View);
            foreach (int familyId in familiesCurrentUserCanAccess)
            {
                if (familiesOtherUserCanAccess.Contains(familyId))
                {
                    // Both users have access to at least one same family.
                    return true;
                }
            }

            List<int> progeniesCurrentUserCanAccess = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, PermissionLevel.View);
            List<int> progeniesOtherUserCanAccess = await accessManagementService.ProgeniesUserCanAccess(otherUserInfo, PermissionLevel.View);
            foreach (int progenyId in progeniesCurrentUserCanAccess)
            {
                if (progeniesOtherUserCanAccess.Contains(progenyId))
                {
                    // Both users have access to at least one same progeny.
                    return true;
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            UserInfo userInfo = await userInfoService.GetUserInfoById(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            bool allowAccess = await CanCurrentUserAccessUserInfo(userEmail, userInfo.UserEmail);
            
            if (allowAccess)
            {
                userInfo.CanUserAddItems = false;
                userInfo.ProgenyList = [];
                userInfo.FamilyList = [];
                List<int> progeniesWithAddPermission = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (progeniesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int progenyId in progeniesWithAddPermission)
                    {
                        Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                        if (progeny != null && progeny.Id != 0)
                        {
                            userInfo.ProgenyList.Add(progeny);
                        }
                    }
                }

                List<int> familiesWithAddPermission = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (familiesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int familyId in familiesWithAddPermission)
                    {
                        Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                        if (family != null && family.FamilyId != 0)
                        {
                            userInfo.FamilyList.Add(family);
                        }
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
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
                userInfo.ProgenyList = [];
                userInfo.FamilyList = [];
                List<int> progeniesWithAddPermission = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (progeniesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int progenyId in progeniesWithAddPermission)
                    {
                        Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                        if (progeny != null && progeny.Id != 0)
                        {
                            userInfo.ProgenyList.Add(progeny);
                        }
                    }
                }

                List<int> familiesWithAddPermission = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, PermissionLevel.Add);
                if (familiesWithAddPermission.Count > 0)
                {
                    userInfo.CanUserAddItems = true;
                    foreach (int familyId in familiesWithAddPermission)
                    {
                        Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                        if (family != null && family.FamilyId != 0)
                        {
                            userInfo.FamilyList.Add(family);
                        }
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
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            // Only allow users to create their own UserInfo entity.
            if (!userEmail.Equals(value.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                return Unauthorized();
            }

            UserInfo userInfo = new()
            {
                ViewChild = value.ViewChild,
                UserEmail = value.UserEmail,
                UserId = value.UserId,
                Timezone = value.Timezone,
                FirstName = value.FirstName,
                MiddleName = value.MiddleName,
                LastName = value.LastName,
                PhoneNumber = value.PhoneNumber,
                ProfilePicture = value.ProfilePicture,
                UserName = value.UserName,
                IsKinaUnaAdmin = false,
                Deleted = false,
                DeletedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            };
            
            UserInfo addedUserInfo = await userInfoService.AddUserInfo(userInfo);
            _ = await userInfoService.SetUserInfoByEmail(userInfo.UserEmail);

            addedUserInfo.CanUserAddItems = false;
            addedUserInfo.ProgenyList = [];
            addedUserInfo.FamilyList = [];
            List<int> progeniesWithAddPermission = await accessManagementService.ProgeniesUserCanAccess(addedUserInfo, PermissionLevel.Add);
            if (progeniesWithAddPermission.Count > 0)
            {
                addedUserInfo.CanUserAddItems = true;
                foreach (int progenyId in progeniesWithAddPermission)
                {
                    Progeny progeny = await progenyService.GetProgeny(progenyId, addedUserInfo);
                    if (progeny != null && progeny.Id != 0)
                    {
                        addedUserInfo.ProgenyList.Add(progeny);
                    }
                }
            }

            List<int> familiesWithAddPermission = await accessManagementService.FamiliesUserCanAccess(addedUserInfo, PermissionLevel.Add);
            if (familiesWithAddPermission.Count > 0)
            {
                addedUserInfo.CanUserAddItems = true;
                foreach (int familyId in familiesWithAddPermission)
                {
                    Family family = await familiesService.GetFamilyById(familyId, addedUserInfo);
                    if (family != null && family.FamilyId != 0)
                    {
                        addedUserInfo.FamilyList.Add(family);
                    }
                }
            }

            return Ok(addedUserInfo);
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

            // Only allow the user themselves, or KinaUna admins, to change their user info.
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

            // Only KinaUnaAdmins can change the IsKinaUnaAdmin property.
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
        /// Also removes the user entries from the permissions tables and MobileNotification table.
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
            
            // Todo: Remove all permissions for user.

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
        [Authorize(Policy = "Client")]
        [HttpGet("[action]/")]
        public async Task<IActionResult> GetDeletedUserInfos()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            if (!currentUserInfo.IsKinaUnaAdmin) return Unauthorized();

            List<UserInfo> deletedUsersList = await userInfoService.GetDeletedUserInfos();
            return Ok(deletedUsersList);

        }

        [Authorize(Policy = "Client")]
        [HttpPost("[action]")]
        public async Task<IActionResult> AddUserInfoToDeletedUserInfos([FromBody] UserInfo userInfo)
        {
            if (userInfo == null || userInfo.Id == 0)
            {
                return BadRequest("Invalid UserInfo object.");
            }
            UserInfo deletedUserInfo = await userInfoService.AddUserInfoToDeletedUserInfos(userInfo);
            if (deletedUserInfo == null)
            {
                return NotFound();
            }
            return Ok(deletedUserInfo);
        }

        [Authorize(Policy = "Client")]
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateDeletedUserInfo([FromBody] UserInfo userInfo)
        {
            if (userInfo == null || userInfo.Id == 0)
            {
                return BadRequest("Invalid UserInfo object.");
            }
            UserInfo updatedUserInfo = await userInfoService.UpdateDeletedUserInfo(userInfo);
            if (updatedUserInfo == null)
            {
                return NotFound();
            }
            return Ok(updatedUserInfo);
        }

        [Authorize(Policy = "Client")]
        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveUserInfoFromDeletedUserInfos([FromBody] UserInfo userInfo)
        {
            UserInfo deletedUserInfo = await userInfoService.RemoveUserInfoFromDeletedUserInfos(userInfo);
            if (deletedUserInfo == null)
            {
                return NotFound();
            }

            return Ok(deletedUserInfo);
        }
    }
}