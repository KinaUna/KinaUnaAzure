using System.Collections.Generic;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserGroupsController(IUserGroupsService userGroupsService, IUserInfoService userInfoService) : ControllerBase
    {
        [HttpGet]
        [Route("[action]/{userGroupId:int}")]
        public async Task<IActionResult> GetUserGroup(int userGroupId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            UserGroup userGroup = await userGroupsService.GetUserGroup(userGroupId, currentUserInfo);
            if (userGroup.UserGroupId == 0)
            {
                return Unauthorized();
            }
            return Ok(userGroup);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetCurrentUsersUserGroups()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            List<UserGroup> userGroups = await userGroupsService.GetUsersUserGroupsByUserId(currentUserInfo.UserId, currentUserInfo);
            return Ok(userGroups);
        }

        [HttpGet]
        [Route("[action]/{progenyId:int}")]
        public async Task<IActionResult> GetUserGroupsForProgeny(int progenyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            List<UserGroup> userGroups = await userGroupsService.GetUserGroupsForProgeny(progenyId, currentUserInfo);

            return Ok(userGroups);
        }

        [HttpGet]
        [Route("[action]/{familyId:int}")]
        public async Task<IActionResult> GetUserGroupsForFamily(int familyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            List<UserGroup> userGroups = await userGroupsService.GetUserGroupsForFamily(familyId, currentUserInfo);
            return Ok(userGroups);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddUserGroup([FromBody] UserGroup userGroup)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            UserGroup newUserGroup = await userGroupsService.AddUserGroup(userGroup, currentUserInfo);
            if (newUserGroup.UserGroupId == 0)
            {
                return Unauthorized();
            }
            return Ok(newUserGroup);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserGroup([FromBody] UserGroup userGroup)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            UserGroup updatedUserGroup = await userGroupsService.UpdateUserGroup(userGroup, currentUserInfo);
            if (updatedUserGroup.UserGroupId == 0)
            {
                return Unauthorized();
            }
            return Ok(updatedUserGroup);
        }

        [HttpDelete]
        [Route("{userGroupId:int}")]
        public async Task<IActionResult> RemoveUserGroup(int userGroupId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            bool result = await userGroupsService.RemoveUserGroup(userGroupId, currentUserInfo);
            if (!result)
            {
                return Unauthorized();
            }
            return Ok(true);
        }

        [HttpPost]
        [Route("AddUserGroupMember")]
        public async Task<IActionResult> AddUserGroupMember([FromBody] UserGroupMember userGroupMember)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            UserGroupMember newUserGroupMember = await userGroupsService.AddUserGroupMember(userGroupMember, currentUserInfo);
            if (newUserGroupMember.UserGroupMemberId == 0)
            {
                return Unauthorized();
            }
            return Ok(newUserGroupMember);
        }

        [HttpPut]
        [Route("UpdateUserGroupMember")]
        public async Task<IActionResult> UpdateUserGroupMember([FromBody] UserGroupMember userGroupMember)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            UserGroupMember updatedUserGroupMember = await userGroupsService.UpdateUserGroupMember(userGroupMember, currentUserInfo);
            if (updatedUserGroupMember.UserGroupMemberId == 0)
            {
                return Unauthorized();
            }
            return Ok(updatedUserGroupMember);
        }

        [HttpDelete]
        [Route("RemoveUserGroupMember/{userGroupMemberId:int}")]
        public async Task<IActionResult> RemoveUserGroupMember(int userGroupMemberId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            bool result = await userGroupsService.RemoveUserGroupMember(userGroupMemberId, currentUserInfo);
            if (!result)
            {
                return Unauthorized();
            }
            return Ok(true);
        }
        
        
    }
}
