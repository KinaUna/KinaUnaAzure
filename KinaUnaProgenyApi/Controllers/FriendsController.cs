using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services.UserAccessService;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Friends.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="friendService"></param>
    /// <param name="timelineService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
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
        /// <summary>
        /// Retrieves the List of all friends for a given Progeny that a user has access to.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Friend items for.</param>
        /// <param name="accessLevel">The user's access level for this Progeny.</param>
        /// <returns>List of Friends.</returns>
        // GET api/friends/progeny/[id]?accessLevel=[accessLevel]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Friend> friendsList = await friendService.GetFriendsList(id, accessLevel);

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

        /// <summary>
        /// Retrieves a Friend entity with a given id.
        /// For mobile clients.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get.</param>
        /// <returns>Friend object with the provided id.</returns>
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

        /// <summary>
        /// Retrieves the list of all Friends for a given Progeny that a user with the give access level has access to.
        /// For mobile clients, as they cannot generate/obtain tokens for accessing image files in Azure storage.
        /// </summary>
        /// <param name="id">The FriendId of the Friend entity to get.</param>
        /// <param name="accessLevel"></param>
        /// <returns>List of Friend items.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Friend> friendsList = await friendService.GetFriendsList(id, accessLevel);
            if (friendsList.Count == 0) return NotFound();

            foreach (Friend friend in friendsList)
            {
                friend.PictureLink = imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);
            }

            return Ok(friendsList);

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
                friend.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Friends, friend.GetPictureFileContentType());
            }

            friend = await friendService.UpdateFriend(friend);

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
