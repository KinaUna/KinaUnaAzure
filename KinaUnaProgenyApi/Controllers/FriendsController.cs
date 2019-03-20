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

        public FriendsController(ProgenyDbContext context, ImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;
        }
        
        // GET api/friends/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == id && f.AccessLevel >= accessLevel).ToListAsync();
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
            Friend result = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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
            friendItem.FriendSince = value.FriendSince;
            friendItem.Notes = value.Notes;
            friendItem.PictureLink = value.PictureLink;
            friendItem.Tags = value.Tags;

            _context.FriendsDb.Update(friendItem);
            await _context.SaveChangesAsync();

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
                _context.FriendsDb.Remove(friendItem);
                await _context.SaveChangesAsync();
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
            Friend result = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                    u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

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
