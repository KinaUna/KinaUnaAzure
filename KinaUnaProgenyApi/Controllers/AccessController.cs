using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for user access management.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController(
        IAzureNotifications azureNotifications,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Gets a list of all UserAccess items for a specific Progeny.
        /// </summary>
        /// <param name="id">The ProgenyId of the progeny to retrieve the list of UserAccess items for.</param>
        /// <returns>List of UserAccess for all users granted access.</returns>
        // GET api/Access/Progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<List<UserAccess>> accessListResult = await userAccessService.GetProgenyUserAccessList(id, userEmail);
            
            return accessListResult.ToActionResult();
        }
        
        /// <summary>
        /// Gets a given UserAccess with the specified id.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess to retrieve.</param>
        /// <returns>UserAccess entity with the specified id.</returns>
        // GET api/Access/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAccess(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess result = await userAccessService.GetUserAccess(id);
            result.Progeny = await progenyService.GetProgeny(result.ProgenyId);

            // Only allow access if the user is an admin for the Progeny or if the UserAccess entity is for the user requesting it.
            if (result.Progeny.IsInAdminList(User.GetEmail()) || result.UserId.Equals(userEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        /// <summary>
        /// Adds a new UserAccess entity to the database.
        /// Then sends notifications to users who are admins for the Progeny.
        /// </summary>
        /// <param name="value">The UserAccess object to add.</param>
        /// <returns>The newly created UserAccess entity.</returns>
        // POST api/Access
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserAccess value)
        {
            value.Progeny = await progenyService.GetProgeny(value.ProgenyId);
            if (value.Progeny != null)
            {
                if (!value.Progeny.IsInAdminList(User.GetEmail()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            UserAccess userAccess = await userAccessService.AddUserAccess(value);
            await NotifyUserAccessAdded(userAccess);

            return Ok(userAccess);
        }

        /// <summary>
        /// Sends notifications to users with admin access to the Progeny when a new UserAccess entity is added.
        /// </summary>
        /// <param name="userAccess">The added UserAccess entity</param>
        /// <returns></returns>
        private async Task NotifyUserAccessAdded(UserAccess userAccess)
        {
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForAdd(userAccess);
            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "User added for " + userAccess.Progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added user: " + userAccess.UserId;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(userAccess, userInfo, notificationTitle);
        }

        /// <summary>
        /// Updates a UserAccess entity with the specified id.
        /// Then sends notifications to users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess entity.</param>
        /// <param name="value">The UserAccess object with the values to update to.</param>
        /// <returns>The updated UserAccess object</returns>
        // PUT api/Access/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] UserAccess value)
        {
            UserAccess originalUserAccess = await userAccessService.GetUserAccess(id);
            value.Progeny = await progenyService.GetProgeny(value.ProgenyId);
            if (value.Progeny != null && originalUserAccess != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!value.Progeny.IsInAdminList(userEmail) || id != value.AccessId)
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            UserAccess updatedUserAccess = await userAccessService.UpdateUserAccess(value);

            if (updatedUserAccess == null)
            {
                return NotFound();
            }

            await NotifyUserAccessUpdated(updatedUserAccess);

            return Ok(updatedUserAccess);
        }

        /// <summary>
        /// Sends notifications to users with admin access to the Progeny when a UserAccess entity is updated.
        /// </summary>
        /// <param name="updatedUserAccess">The updated UserAccess object.</param>
        /// <returns></returns>
        private async Task NotifyUserAccessUpdated(UserAccess updatedUserAccess)
        {
            string notificationTitle = "User access modified for " + updatedUserAccess.Progeny.NickName;
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            if (userInfo == null) return;

            string notificationMessage = userInfo.FullName() + " modified access to " + updatedUserAccess.Progeny.NickName + " for user: " + updatedUserAccess.UserId;

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForUpdate(updatedUserAccess);

            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(updatedUserAccess, userInfo, notificationTitle);
        }

        /// <summary>
        /// Deletes a UserAccess entity with the specified id.
        /// Then sends notifications to users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess entity to delete.</param>
        /// <returns>NoContentResult, or if the item is not found NotFoundResult.</returns>
        // DELETE api/Access/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        { 
            UserAccess userAccess = await userAccessService.GetUserAccess(id);
            if (userAccess == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            userAccess.Progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
            if (userAccess.Progeny != null)
            {
                if (!userAccess.Progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            await userAccessService.RemoveUserAccess(userAccess.AccessId, userAccess.ProgenyId, userAccess.UserId);

            await NotifyUserAccessDeleted(userAccess);

            return NoContent();

        }

        /// <summary>
        /// Sends notifications to users with admin access to the Progeny when a UserAccess entity is deleted.
        /// </summary>
        /// <param name="userAccess">The deleted UserAccess object.</param>
        /// <returns></returns>
        private async Task NotifyUserAccessDeleted(UserAccess userAccess)
        {
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "User removed for " + userAccess.Progeny.NickName;
            string notificationMessage = userInfo.FullName() + " removed access to " + userAccess.Progeny.NickName + " for user: " + userAccess.UserId;
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForUpdate(userAccess);
            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(userAccess, userInfo, notificationTitle);
        }
        
        /// <summary>
        /// Retrieves the list of UserAccess items for a given user.
        /// </summary>
        /// <param name="userEmail">The user's email address</param>
        /// <returns>List of UserAccess items for the user.</returns>
        // POST api/Access/AccessListByUser/
        [HttpPost("[action]")]
        public async Task<IActionResult> AccessListByUser([FromBody] string userEmail)
        {
            if (!userEmail.Equals(User.GetEmail() ?? Constants.DefaultUserEmail, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(userEmail);
            
            foreach (UserAccess userAccess in userAccessList)
            {
                userAccess.Progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
            }

            return Ok(userAccessList);
        }
        
        /// <summary>
        /// Retrieves the list of Progeny that a user has admin access to.
        /// </summary>
        /// <param name="id">The email address of th user.</param>
        /// <returns>List of Progeny that the user is admin for.</returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> AdminListByUserPost([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return Ok();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAdminAccessList(id);
            List<Progeny> progenyList = [];
            
            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                progenyList.Add(progeny);
            }

            return Ok(progenyList);
        }

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
            if (!model.OldEmail.Equals(userInfo.UserEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound("User with given id and email could not be found.");
            }
            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(model.OldEmail);
            foreach (UserAccess userAccess in userAccessList)
            {
                userAccess.UserId = model.NewEmail;
                await userAccessService.UpdateUserAccess(userAccess);
            }



            List<Progeny> progenyList = await progenyService.GetAllProgenies();
            progenyList = [.. progenyList.Where(p => p.IsInAdminList(model.OldEmail))];
            
            if (progenyList.Count == 0) return Ok();

            foreach (Progeny prog in progenyList)
            {
                string adminList = prog.Admins.ToUpper();
                prog.Admins = adminList.Replace(model.OldEmail.ToUpper(), model.NewEmail.ToUpper());
                await progenyService.UpdateProgeny(prog);
            }

            return Ok();
        }
    }
}
