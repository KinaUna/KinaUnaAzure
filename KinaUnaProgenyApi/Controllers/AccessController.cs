using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for user access management.
    /// </summary>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController(
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IAccessManagementService accessManagementService,
        IUserGroupsService userGroupsService,
        IFamiliesService familiesService,
        IFamilyMembersService familyMembersService)
        : ControllerBase
    {
        /// <summary>
        /// Updates a user's email address across multiple related entities.
        /// </summary>
        /// <remarks>This method updates the user's email address in the following places: <list
        /// type="bullet"> <item><description>The user's access list, replacing the old email with the new
        /// email.</description></item> <item><description>The admin list of any related entities (e.g., progenies)
        /// where the old email is present.</description></item> </list> If no related entities are found for the old
        /// email, the method completes successfully without making further updates.</remarks>
        /// <param name="model">An instance of <see cref="UpdateUserEmailModel"/> containing the user's ID, the old email address, and the
        /// new email address to update.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="NotFoundResult"/>
        /// if the user with the specified ID and old email address cannot be found. Returns <see cref="OkResult"/> if
        /// the update is successful.</returns>
        [Authorize(Policy = "Client")]
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateUsersEmail([FromBody] UpdateUserEmailModel model)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoByUserId(model.UserId);
            // Verify that the user exists and the old email matches.
            if (!model.OldEmail.Equals(userInfo.UserEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound("User with given id and email could not be found.");
            }
            
            // Update permissions.
            await accessManagementService.ChangeUsersEmailForPermissions(userInfo, model.NewEmail);
            
            // Update user groups.
            await userGroupsService.ChangeUsersEmailForGroupMembers(userInfo, model.NewEmail);
            
            // Update family members.
            await familyMembersService.ChangeUsersEmailForFamilyMembers(userInfo, model.NewEmail);
            
            // Update families where the user is an admin.
            await familiesService.ChangeUsersEmailForFamilies(userInfo, model.NewEmail);

            // Update progenies where the user is an admin or is the progeny
            await progenyService.ChangeUsersEmailForProgenies(userInfo, model.NewEmail);

            return Ok();
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> ConvertUserAccessesToUserGroups()
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null || !currentUserInfo.IsKinaUnaAdmin) return Unauthorized();

            await userAccessService.ConvertUserAccessesToUserGroups();
            return Ok();
        }

        [HttpGet]
        [Route("[action]/{itemType:int}")]
        public async Task<IActionResult> ConvertItemAccessLevelToItemPermissions(int itemType)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null || !currentUserInfo.IsKinaUnaAdmin) return Unauthorized();
            bool moreItem = true;
            while (moreItem)
            {
                moreItem = await userAccessService.ConvertItemAccessLevelToItemPermissionsForGroups((KinaUnaTypes.TimeLineType)itemType, 100);
            }
            
            return Ok();
        }
    }
}
