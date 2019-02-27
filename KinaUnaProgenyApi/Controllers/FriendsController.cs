using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
        // GET api/friends
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Friend> resultList = await _context.FriendsDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/friends/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == id && f.AccessLevel >= accessLevel).ToListAsync();
            if (friendsList.Any())
            {
                return Ok(friendsList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/friends/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFriendItem(int id)
        {
            Friend result = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);

            return Ok(result);
        }

        // POST api/friends
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Friend value)
        {
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
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "friends");
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient friendsHttpClient = new HttpClient();
            
            friendsHttpClient.BaseAddress = new Uri("https://kinauna.com");
            friendsHttpClient.DefaultRequestHeaders.Accept.Clear();
            friendsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string friendsApiPath = "/api/azureexport/friendsexport";
            var friendsUri = "https://kinauna.com" + friendsApiPath;

            var friendsResponseString = await friendsHttpClient.GetStringAsync(friendsUri);

            List<Friend> friendsList = JsonConvert.DeserializeObject<List<Friend>>(friendsResponseString);
            List<Friend> friendsItems = new List<Friend>();
            foreach (Friend value in friendsList)
            {
                Friend friendItem = new Friend();
                friendItem.AccessLevel = value.AccessLevel;
                friendItem.Author = value.Author;
                friendItem.Context = value.Context;
                friendItem.Name = value.Name;
                friendItem.Type = value.Type;
                friendItem.FriendAddedDate = value.FriendAddedDate;
                friendItem.ProgenyId = value.ProgenyId;
                friendItem.Description = value.Description;
                friendItem.FriendSince = value.FriendSince;
                friendItem.Notes = value.Notes;
                friendItem.PictureLink = "https://" + value.PictureLink;
                friendItem.Tags = value.Tags;
                await _context.FriendsDb.AddAsync(friendItem);
                friendsItems.Add(friendItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(friendsItems);
        }

        [HttpGet]
        [Route("[action]/{friendId}")]
        public async Task<IActionResult> DownloadPicture(int friendId)
        {
            Friend friend = await _context.FriendsDb.SingleOrDefaultAsync(c => c.FriendId == friendId);
            if (friend != null && friend.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = GetStreamFromUrl(friend.PictureLink))
                {
                    friend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                }

                _context.FriendsDb.Update(friend);
                await _context.SaveChangesAsync();
                return Ok(friend);
            }
            else
            {
                return NotFound();
            }
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
