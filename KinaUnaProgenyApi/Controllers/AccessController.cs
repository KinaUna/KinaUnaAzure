using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController(
        IImageStore imageStore,
        IAzureNotifications azureNotifications,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/Access/Progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(id);

            if (accessList.Count == 0) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            bool allowedAccess = false;
            foreach (UserAccess ua in accessList)
            {
                ua.Progeny = await progenyService.GetProgeny(ua.ProgenyId);

                ua.User = new ApplicationUser();
                UserInfo userinfo = await userInfoService.GetUserInfoByEmail(ua.UserId);
                if (userinfo != null)
                {
                    ua.User.FirstName = userinfo.FirstName;
                    ua.User.MiddleName = userinfo.MiddleName;
                    ua.User.LastName = userinfo.LastName;
                    ua.User.UserName = userinfo.UserName;
                }
                ua.User.Email = ua.UserId;
                if (ua.User.Email.Equals(userEmail, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    allowedAccess = true;
                }
            }

            if (!allowedAccess && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            return Ok(accessList);

        }

        // GET api/Access/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAccess(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess result = await userAccessService.GetUserAccess(id);
            result.Progeny = await progenyService.GetProgeny(result.ProgenyId);

            if (result.Progeny.IsInAdminList(User.GetEmail()) || result.UserId.Equals(userEmail, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return Ok(result);
            }

            return Unauthorized();
        }

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

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForAdd(userAccess);
            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            string notificationTitle = "User added for " + userAccess.Progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added user: " + userAccess.UserId;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(userAccess, userInfo, notificationTitle);

            return Ok(userAccess);
        }

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

            string notificationTitle = "User access modified for " + updatedUserAccess.Progeny.NickName;
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(User.GetEmail());
            if (userInfo == null) return Ok(updatedUserAccess);

            string notificationMessage = userInfo.FullName() + " modified access to " + value.Progeny.NickName + " for user: " + updatedUserAccess.UserId;

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForUpdate(updatedUserAccess);

            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(updatedUserAccess, userInfo, notificationTitle);

            return Ok(updatedUserAccess);
        }

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


            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "User removed for " + userAccess.Progeny.NickName;
            string notificationMessage = userInfo.FullName() + " removed access to " + userAccess.Progeny.NickName + " for user: " + userAccess.UserId;
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyUserAccessItemPropertiesForUpdate(userAccess);
            timeLineItem.AccessLevel = 0; // Only admins should be notified of changes to user access.

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendUserAccessNotification(userAccess, userInfo, notificationTitle);

            return NoContent();

        }

        // GET api/Access/ProgenyListByUser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<Progeny> result = [];
            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(id);

            if (userAccessList.Count == 0) return NotFound();

            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                if (string.IsNullOrEmpty(progeny.PictureLink))
                {
                    progeny.PictureLink = Constants.ProfilePictureUrl;
                }
                result.Add(progeny);
            }

            return Ok(result);

        }

        // GET api/Access/ProgenyListByUserMobile/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUserMobile(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<Progeny> result = [];
            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(id);

            if (userAccessList.Count == 0) return NotFound();

            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                if (string.IsNullOrEmpty(progeny.PictureLink))
                {
                    progeny.PictureLink = Constants.ProfilePictureUrl;
                }

                progeny.PictureLink = imageStore.UriFor(progeny.PictureLink, "progeny");

                result.Add(progeny);
            }

            return Ok(result);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ProgenyListByUserPostMobile([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<Progeny> result = [];
            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(id);

            if (userAccessList.Count == 0) return NotFound();

            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                if (string.IsNullOrEmpty(progeny.PictureLink))
                {
                    progeny.PictureLink = Constants.ProfilePictureUrl;
                }

                progeny.PictureLink = imageStore.UriFor(progeny.PictureLink, "progeny");

                result.Add(progeny);
            }

            return Ok(result);

        }

        // GET api/Access/AccessListByUser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AccessListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(id);
            if (userAccessList.Count == 0) return NotFound();

            foreach (UserAccess userAccess in userAccessList)
            {
                userAccess.Progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
            }
            return Ok(userAccessList);

        }

        // GET api/Access/AdminListByUser/[UserEmail]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AdminListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return Ok();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAdminAccessList(id);

            List<Progeny> progenyList = [];
            if (userAccessList.Count == 0) return Ok(progenyList);

            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                progenyList.Add(progeny);
            }
            return Ok(progenyList);

        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AdminListByUserPost([FromBody] string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(id, System.StringComparison.CurrentCultureIgnoreCase)) return Ok();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAdminAccessList(id);
            List<Progeny> progenyList = [];
            if (userAccessList.Count == 0) return Ok(progenyList);

            foreach (UserAccess userAccess in userAccessList)
            {
                Progeny progeny = await progenyService.GetProgeny(userAccess.ProgenyId);
                progenyList.Add(progeny);
            }

            return Ok(progenyList);

        }

        [HttpGet("[action]/{oldEmail}/{newEmail}")]
        public async Task<IActionResult> UpdateAccessListEmailChange(string oldEmail, string newEmail)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (!userEmail.Equals(oldEmail, System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            List<UserAccess> userAccessList = await userAccessService.GetUsersUserAccessList(oldEmail);
            if (userAccessList.Count == 0) return NotFound();

            foreach (UserAccess userAccess in userAccessList)
            {
                userAccess.UserId = newEmail;
                await userAccessService.UpdateUserAccess(userAccess);
            }

            return Ok(userAccessList);

        }
    }
}
