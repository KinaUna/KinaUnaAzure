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
    public class FriendsController : ControllerBase
    {
        private readonly ImageStore _imageStore;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly AzureNotifications _azureNotifications;
        private readonly IFriendService _friendService;
        private readonly ITimelineService _timelineService;
        private readonly IProgenyService _progenyService;

        public FriendsController(ImageStore imageStore, AzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            IFriendService friendService, ITimelineService timelineService, IProgenyService progenyService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _friendService = friendService;
            _timelineService = timelineService;
            _progenyService = progenyService;
        }

        // GET api/friends/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Friend> friendsList = await _friendService.GetFriendsList(id);
                friendsList = friendsList.Where(f => f.AccessLevel >= accessLevel).ToList();
                if (friendsList.Any())
                {
                    return Ok(friendsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/friends/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFriendItem(int id)
        {
            Friend result = await _friendService.GetFriend(id);
            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
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

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }
            
            Friend friendItem = await _friendService.AddFriend(value);
            
            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.CopyFriendPropertiesForAdd(friendItem);
            await _timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Friend added for " + prog.NickName;
            string notificationMessage = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new friend for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userinfo.ProfilePicture);

            return Ok(friendItem);
        }

        // PUT api/friends/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Friend value)
        {
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
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

            if (value.PictureLink == Constants.DefaultPictureLink)
            {
                value.PictureLink = Constants.ProfilePictureUrl;
            }

            Friend friendItem = await _friendService.GetFriend(id);
            if (friendItem == null)
            {
                return NotFound();
            }

            friendItem.CopyPropertiesForUpdate(value);
            
            friendItem = await _friendService.UpdateFriend(friendItem);
            
            // Update timeline
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend);
            if (timeLineItem != null && timeLineItem.CopyFriendItemPropertiesForUpdate(friendItem))
            {
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Friend edited for " + prog.NickName;
            string notificationMessage = userinfo.FullName() + " edited a friend for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userinfo.ProfilePicture);

            return Ok(friendItem);
        }

        // DELETE api/friends/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Friend friendItem = await _friendService.GetFriend(id);
            if (friendItem != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                // Check if child exists.
                Progeny prog = await _progenyService.GetProgeny(friendItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete contacts for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                // Remove item from timeline.
                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(friendItem.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend);
                
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _imageStore.DeleteImage(friendItem.PictureLink, BlobContainers.Friends);

                _ = await _friendService.DeleteFriend(friendItem);
                
                if (timeLineItem != null)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                    string notificationTitle = "Friend deleted for " + prog.NickName;
                    string notificationMessage = userInfo.FullName() + " deleted a friend for " + prog.NickName + ". Friend: " + friendItem.Name;
                    timeLineItem.AccessLevel = 0;
                    
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetFriendMobile(int id)
        {
            Friend result = await _friendService.GetFriend(id); 

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); 

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Friends);

                    return Ok(result);
                }
            }

            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Friend> friendsList = await _friendService.GetFriendsList(id);
                friendsList = friendsList.Where(f => f.AccessLevel >= accessLevel).ToList();
                if (friendsList.Any())
                {
                    foreach (Friend friend in friendsList)
                    {
                        friend.PictureLink = _imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);
                    }

                    return Ok(friendsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{friendId}")]
        public async Task<IActionResult> DownloadPicture(int friendId)
        {
            Friend friend = await _friendService.GetFriend(friendId);

            if (friend == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(friend.ProgenyId, userEmail);


            if (userAccess != null && userAccess.AccessLevel > 0 && friend.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = await GetStreamFromUrl(friend.PictureLink))
                {
                    friend.PictureLink = await _imageStore.SaveImage(stream, BlobContainers.Friends);
                }

                friend = await _friendService.UpdateFriend(friend);

                return Ok(friend);
            }


            return NotFound();
            
        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            return streamToReadFrom;
        }
    }
}
