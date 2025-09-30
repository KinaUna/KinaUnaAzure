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
        public async Task<IActionResult> ProgeniesUserCanAccessList(PermissionLevel permissionLevel)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<int> progenyIds = await accessManagementService.ProgeniesUserCanAccess(currentUserInfo, permissionLevel);

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
        public async Task<IActionResult> FamiliesUserCanAccessList(PermissionLevel permissionLevel)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<int> familyIds = await accessManagementService.FamiliesUserCanAccess(currentUserInfo, permissionLevel);

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
    }
}
