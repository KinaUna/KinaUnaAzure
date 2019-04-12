using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FriendsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly ImageStore _imageStore;
        private readonly IDataService _dataService;

        public FriendsController(ProgenyDbContext context, ImageStore imageStore, IDataService dataService)
        {
            _context = context;
            _imageStore = imageStore;
            _dataService = dataService;
        }
        
        // GET api/friends/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Friend> friendsList = await _dataService.GetFriendsList(id); // await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == id && f.AccessLevel >= accessLevel).ToListAsync();
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
            Friend result = await _dataService.GetFriend(id); // await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add contacts for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Friend friendItem = new Friend();
            friendItem.AccessLevel = value.AccessLevel;
            friendItem.Author = value.Author;
            friendItem.Context = value.Context;
            friendItem.Name = value.Name;
            friendItem.Type = value.Type;
            friendItem.FriendAddedDate = DateTime.UtcNow;
            friendItem.ProgenyId = value.ProgenyId;
            friendItem.Description = value.Description;
            friendItem.FriendSince = value.FriendSince;
            friendItem.Notes = value.Notes;
            friendItem.PictureLink = value.PictureLink;
            friendItem.Tags = value.Tags;
            
            _context.FriendsDb.Add(friendItem);
            await _context.SaveChangesAsync();
            await _dataService.SetFriend(friendItem.FriendId);

            // Add item to Timeline.
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = friendItem.ProgenyId;
            tItem.AccessLevel = friendItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Friend;
            tItem.ItemId = friendItem.FriendId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            tItem.CreatedBy = userinfo?.UserId ?? "User Not Found";
            tItem.CreatedTime = DateTime.UtcNow;
            if (friendItem.FriendSince != null)
            {
                tItem.ProgenyTime = friendItem.FriendSince.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            return Ok(friendItem);
        }

        // PUT api/friends/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Friend value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add contacts for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Friend friendItem = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == id);
            if (friendItem == null)
            {
                return NotFound();
            }

            friendItem.AccessLevel = value.AccessLevel;
            friendItem.Author = value.Author;
            friendItem.Context = value.Context;
            friendItem.Name = value.Name;
            friendItem.Type = value.Type;
            friendItem.FriendAddedDate = DateTime.UtcNow;
            friendItem.ProgenyId = value.ProgenyId;
            friendItem.Description = value.Description;
            friendItem.FriendSince = value.FriendSince ?? DateTime.UtcNow;
            friendItem.Notes = value.Notes;
            friendItem.PictureLink = value.PictureLink;
            friendItem.Tags = value.Tags;

            _context.FriendsDb.Update(friendItem);
            await _context.SaveChangesAsync();
            await _dataService.SetFriend(friendItem.FriendId);

            // Update timeline
            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == friendItem.FriendId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Friend);
            if (tItem != null)
            {
                tItem.ProgenyTime = friendItem.FriendSince.Value;
                tItem.AccessLevel = friendItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            return Ok(friendItem);
        }

        // DELETE api/friends/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Friend friendItem = await _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == id);
            if (friendItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == friendItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete contacts for this child.
                    string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                // Remove item from timeline.
                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == friendItem.FriendId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Friend);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                // Remove picture
                if (!friendItem.PictureLink.ToLower().StartsWith("http"))
                {
                    await _imageStore.DeleteImage(friendItem.PictureLink, "friends");
                }

                _context.FriendsDb.Remove(friendItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveFriend(friendItem.FriendId, friendItem.ProgenyId);
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
            Friend result = await _dataService.GetFriend(id); // await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    if (!result.PictureLink.ToLower().StartsWith("http"))
                    {
                        result.PictureLink = _imageStore.UriFor(result.PictureLink, "friends");
                    }
                    return Ok(result);
                }
            }

            return NotFound();
        }

        
        [HttpGet]
        [Route("[action]/{friendId}")]
        public async Task<IActionResult> DownloadPicture(int friendId)
        {
            Friend friend = await _context.FriendsDb.SingleOrDefaultAsync(c => c.FriendId == friendId);

            if (friend == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == friend.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());


            if (userAccess != null && userAccess.AccessLevel > 0 && friend.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = GetStreamFromUrl(friend.PictureLink))
                {
                    friend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                }

                _context.FriendsDb.Update(friend);
                await _context.SaveChangesAsync();
                return Ok(friend);
            }


            return NotFound();
            
        }

        private static Stream GetStreamFromUrl(string url)
        {
            byte[] imageData;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new MemoryStream(imageData);
        }
    }
}
