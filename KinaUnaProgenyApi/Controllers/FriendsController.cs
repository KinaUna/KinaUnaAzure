using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Friends.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="userInfoService"></param>
    /// <param name="friendService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendsController(
        IImageStore imageStore,
        IUserInfoService userInfoService,
        IFriendService friendService,
        ITimelineService timelineService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService,
        IAccessManagementService accessManagementService)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves the List of all friends for a given Progeny that a user has access to.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Friend items for.</param>
        /// <returns>List of Friends.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<Friend> friendsList = await friendService.GetFriendsList(id, currentUserInfo);

            return Ok(friendsList);

        }

        /// <summary>
        /// Retrieves a Friend item with a given id.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity.</param>
        /// <returns>The Friend object with the provided id. NotFoundResult if the entity doesn't exist, UnauthorizedResult if the user doesn't have the access rights to the Friend entity.</returns>
        // GET api/friends/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFriendItem(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Friend friend = await friendService.GetFriend(id, currentUserInfo);
            if (friend == null)
            {
                return NotFound();
            }
            
            return Ok(friend);
        }

        /// <summary>
        /// Adds a new Friend entity to the database.
        /// Then adds a TimeLineItem for the Friend entity.
        /// Then sends notifications to users with access to the Friend item.
        /// </summary>
        /// <param name="value">The Friend object to add.</param>
        /// <returns>The added Friend object if successful. Unauthorized if the user isn't allowed to add items for the Progeny. NotFoundResult if the Progeny doesn't exist.</returns>
        // POST api/friends
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Friend value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (!await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                Unauthorized();
            }

            value.Author = User.GetUserId();

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            value.CreatedBy = User.GetUserId();
            value.ModifiedBy = User.GetUserId();

            Friend friendItem = await friendService.AddFriend(value, currentUserInfo);
            if (friendItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyFriendPropertiesForAdd(friendItem);
            await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);
            Progeny progeny = await progenyService.GetProgeny(friendItem.ProgenyId, currentUserInfo);
            string notificationTitle = "Friend added for " + progeny.NickName; // Todo: Localize.
            await webNotificationsService.SendFriendNotification(friendItem, currentUserInfo, notificationTitle);

            friendItem = await friendService.GetFriend(friendItem.FriendId, currentUserInfo);

            return Ok(friendItem);
        }

        /// <summary>
        /// Updates an existing Friend entity in the database.
        /// Then updates the TimeLineItem for the Friend entity.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to update.</param>
        /// <param name="value">Friend object with the properties to update.</param>
        /// <returns>The updated Friend object.</returns>
        // PUT api/friends/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Friend value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, id, currentUserInfo, PermissionLevel.Edit))
            {
                return Unauthorized();
            }

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            Friend friendItem = await friendService.GetFriend(id, currentUserInfo);
            if (friendItem == null)
            {
                return NotFound();
            }

            value.ModifiedBy = User.GetUserId();
            
            friendItem.CopyPropertiesForUpdate(value);

            friendItem = await friendService.UpdateFriend(friendItem, currentUserInfo);
            if (friendItem == null)
            {
                return Unauthorized();
            }

            friendItem.Author = User.GetUserId();

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, currentUserInfo);
            if (timeLineItem != null && timeLineItem.CopyFriendItemPropertiesForUpdate(friendItem))
            {
                _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);
            }

            friendItem = await friendService.GetFriend(friendItem.FriendId, currentUserInfo);

            return Ok(friendItem);
        }

        /// <summary>
        /// Deletes a Friend entity from the database.
        /// Then deletes the TimeLineItem for the Friend entity.
        /// Then sends a notification to all users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to delete.</param>
        /// <returns>No content if successfully deleted. UnauthorizedResult if the user doesn't have the required access level. NotFoundResult if the entity doesn't exist.</returns>
        // DELETE api/friends/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, id, currentUserInfo, PermissionLevel.Admin))
            {
                return Unauthorized();
            }

            Friend friendItem = await friendService.GetFriend(id, currentUserInfo);
            if (friendItem == null) return NotFound();
            
            Progeny progeny = await progenyService.GetProgeny(friendItem.ProgenyId, currentUserInfo);
            
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend, currentUserInfo);
            
            friendItem.ModifiedBy = User.GetUserId();

            Friend deletedFriend = await friendService.DeleteFriend(friendItem, currentUserInfo);
            if (deletedFriend == null)
            {
                return Unauthorized();
            }

            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }

            if (timeLineItem == null) return NoContent();

            friendItem.Author = User.GetUserId();
            
            string notificationTitle = "Friend deleted for " + progeny.NickName; // Todo: Localize.
            
            friendItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await webNotificationsService.SendFriendNotification(friendItem, currentUserInfo, notificationTitle);

            return NoContent();

        }
        
        /// <summary>
        /// Download a Friend's profile picture from a URL in the Friend's PictureLink and save it to the image store.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend entity.</param>
        /// <returns>The Friend entity with the updated PictureLink.</returns>
        [HttpGet]
        [Route("[action]/{friendId:int}")]
        public async Task<IActionResult> DownloadPicture(int friendId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Friend friend = await friendService.GetFriend(friendId, currentUserInfo);

            if (friend == null || friend.FriendId == 0 || friend.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return NotFound();
            }

            if (!friend.PictureLink.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase)) return NotFound();

            await using (Stream stream = await GetStreamFromUrl(friend.PictureLink))
            {
                friend.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Friends, friend.GetPictureFileContentType());
            }

            friend = await friendService.UpdateFriend(friend, currentUserInfo);

            return Ok(friend);


        }

        //Todo: Refactor, this method is also in the ContactsController.
        /// <summary>
        /// Helper method to get a Stream from a URL.
        /// </summary>
        /// <param name="url">The url to get the stream for.</param>
        /// <returns>The stream object for the URL.</returns>
        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
