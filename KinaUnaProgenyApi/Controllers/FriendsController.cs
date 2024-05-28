using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Extensions;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendsController(
        IImageStore imageStore,
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        IFriendService friendService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/friends/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Friend> friendsList = await friendService.GetFriendsList(id);
            friendsList = friendsList.Where(f => f.AccessLevel >= accessLevel).ToList();
            if (friendsList.Count != 0)
            {
                return Ok(friendsList);
            }
            return NotFound();

        }

        // GET api/friends/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFriendItem(int id)
        {
            Friend result = await friendService.GetFriend(id);
            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/friends
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Friend value)
        {
            // Check if child exists.
            Progeny prog = await progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add contacts for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            value.Author = User.GetUserId();

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            Friend friendItem = await friendService.AddFriend(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyFriendPropertiesForAdd(friendItem);
            await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Friend added for " + prog.NickName;
            string notificationMessage = userInfo.FirstName + " " + userInfo.MiddleName + " " + userInfo.LastName + " added a new friend for " + prog.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendFriendNotification(friendItem, userInfo, notificationTitle);

            return Ok(friendItem);
        }

        // PUT api/friends/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Friend value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            Friend friendItem = await friendService.GetFriend(id);
            if (friendItem == null)
            {
                return NotFound();
            }

            friendItem.CopyPropertiesForUpdate(value);

            friendItem = await friendService.UpdateFriend(friendItem);

            friendItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend);
            if (timeLineItem != null && timeLineItem.CopyFriendItemPropertiesForUpdate(friendItem))
            {
                _ = await timelineService.UpdateTimeLineItem(timeLineItem);
            }

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Friend edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a friend for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendFriendNotification(friendItem, userInfo, notificationTitle);

            return Ok(friendItem);
        }

        // DELETE api/friends/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Friend friendItem = await friendService.GetFriend(id);
            if (friendItem == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Progeny progeny = await progenyService.GetProgeny(friendItem.ProgenyId);
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend);

            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await imageStore.DeleteImage(friendItem.PictureLink, BlobContainers.Friends);

            _ = await friendService.DeleteFriend(friendItem);

            if (timeLineItem == null) return NoContent();

            friendItem.Author = User.GetUserId();
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Friend deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a friend for " + progeny.NickName + ". Friend: " + friendItem.Name;

            friendItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendFriendNotification(friendItem, userInfo, notificationTitle);

            return NoContent();

        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetFriendMobile(int id)
        {
            Friend result = await friendService.GetFriend(id);

            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId) return NotFound();

            result.PictureLink = imageStore.UriFor(result.PictureLink, BlobContainers.Friends);

            return Ok(result);

        }

        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Friend> friendsList = await friendService.GetFriendsList(id);
            friendsList = friendsList.Where(f => f.AccessLevel >= accessLevel).ToList();
            if (friendsList.Count == 0) return NotFound();

            foreach (Friend friend in friendsList)
            {
                friend.PictureLink = imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);
            }

            return Ok(friendsList);

        }

        [HttpGet]
        [Route("[action]/{friendId:int}")]
        public async Task<IActionResult> DownloadPicture(int friendId)
        {
            Friend friend = await friendService.GetFriend(friendId);

            if (friend == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(friend.ProgenyId, userEmail);
            if (userAccess == null || userAccess.AccessLevel <= 0 || !friend.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            await using (Stream stream = await GetStreamFromUrl(friend.PictureLink))
            {
                friend.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Friends);
            }

            friend = await friendService.UpdateFriend(friend);

            return Ok(friend);


        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
