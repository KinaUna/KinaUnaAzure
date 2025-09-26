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
    public class AccessManagementController(IAccessManagementService accessManagementService, IUserInfoService userInfoService) : ControllerBase
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
    }
}
