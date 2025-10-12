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
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessManagementController(IAccessManagementService accessManagementService,
        IUserInfoService userInfoService,
        IProgenyService progenyService,
        IFamiliesService familiesService) : ControllerBase
    {
        [HttpGet]
        [Route("[action]/{progenyId:int}")]
        public async Task<IActionResult> UserCanAddItemsForProgeny(int progenyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            bool hasAccess = await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.Add);
            
            return Ok(hasAccess);
        }

        [HttpGet]
        [Route("[action]/{familyId:int}")]
        public async Task<IActionResult> UserCanAddItemsForFamily(int familyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            bool hasAccess = await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.Add);

            return Ok(hasAccess);
        }

        [HttpGet]
        [Route("[action]/{permissionLevel:int}")]
        public async Task<IActionResult> ProgeniesUserCanAccessList(int permissionLevel)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<int> progenyIds = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, (PermissionLevel)permissionLevel);

            List<Progeny> progenies = [];
            foreach (int progenyId in progenyIds)
            {
                Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                if (progeny != null)
                {
                    progenies.Add(progeny);
                }
            }

            return Ok(progenies);
        }

        [HttpGet]
        [Route("[action]/{permissionLevel:int}")]
        public async Task<IActionResult> FamiliesUserCanAccessList(int permissionLevel)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<int> familyIds = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, (PermissionLevel)permissionLevel);

            List<Family> families = [];
            foreach (int familyId in familyIds)
            {
                Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                if (family != null)
                {
                    families.Add(family);
                }
            }

            return Ok(families);
        }

        [HttpGet]
        [Route("[action]/{itemType:int}/{itemId:int}/{progenyId:int}/{familyId:int}")]
        public async Task<IActionResult> GetItemPermissionForUser(int itemType, int itemId, int progenyId, int familyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TimelineItemPermission permission = await accessManagementService.GetItemPermissionForUser((KinaUnaTypes.TimeLineType)itemType, itemId, progenyId, familyId, currentUserInfo);
            return Ok(permission.PermissionLevel);
        }

        [HttpGet]
        [Route("[action]/{itemType:int}/{itemId:int}")]
        public async Task<IActionResult> GetTimelineItemPermissionsList(int itemType, int itemId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<TimelineItemPermission> permissions = await accessManagementService.GetTimelineItemPermissionsList((KinaUnaTypes.TimeLineType)itemType, itemId, currentUserInfo);
            
            return Ok(permissions);
        }

        [HttpGet]
        [Route("[action]/{progenyId:int}")]
        public async Task<List<ProgenyPermission>> GetProgenyPermissionsList(int progenyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<ProgenyPermission> permissions = await accessManagementService.GetProgenyPermissionsList(progenyId, currentUserInfo);
            foreach (ProgenyPermission progenyPermission in permissions)
            {
                if (!string.IsNullOrWhiteSpace(progenyPermission.UserId))
                {
                    UserInfo userInfo = await userInfoService.GetUserInfoByUserId(progenyPermission.UserId);
                    progenyPermission.UserInfo = userInfo;
                }
            }
            return permissions;
        }

        [HttpGet]
        [Route("[action]/{familyId:int}")]
        public async Task<List<FamilyPermission>> GetFamilyPermissionsList(int familyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<FamilyPermission> permissions = await accessManagementService.GetFamilyPermissionsList(familyId, currentUserInfo);
            foreach (FamilyPermission familyPermission in permissions)
            {
                if (!string.IsNullOrWhiteSpace(familyPermission.UserId))
                {
                    UserInfo userInfo = await userInfoService.GetUserInfoByUserId(familyPermission.UserId);
                    familyPermission.UserInfo = userInfo;
                }
            }
            return permissions;
        }

        [HttpGet]
        [Route("[action]/{permissionId:int}")]
        public async Task<IActionResult> GetFamilyPermission(int permissionId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            FamilyPermission permission = await accessManagementService.GetFamilyPermission(permissionId, currentUserInfo);

            if (permission != null && !string.IsNullOrWhiteSpace(permission.UserId))
            {
                UserInfo userInfo = await userInfoService.GetUserInfoByUserId(permission.UserId);
                permission.UserInfo = userInfo;
            }
            
            return Ok(permission);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddFamilyPermission([FromBody] FamilyPermission familyPermission)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            FamilyPermission addedPermission = await accessManagementService.GrantFamilyPermission(familyPermission, currentUserInfo);
            return Ok(addedPermission);

        }

        [HttpGet]
        [Route("[action]/{permissionId:int}")]
        public async Task<IActionResult> GetProgenyPermission(int permissionId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            ProgenyPermission permission = await accessManagementService.GetProgenyPermission(permissionId, currentUserInfo);
            if (permission != null && !string.IsNullOrWhiteSpace(permission.UserId))
            {
                UserInfo userInfo = await userInfoService.GetUserInfoByUserId(permission.UserId);
                permission.UserInfo = userInfo;
            }
            return Ok(permission);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddProgenyPermission([FromBody] ProgenyPermission progenyPermission)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            ProgenyPermission addedPermission = await accessManagementService.GrantProgenyPermission(progenyPermission, currentUserInfo);
            return Ok(addedPermission);
        }
    }
}
