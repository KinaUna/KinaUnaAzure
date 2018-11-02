using KinaUnaMediaApi.Data;
using KinaUnaMediaApi.Models;
using KinaUnaMediaApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly MediaDbContext _context;
        public CommentsController(MediaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Comment> resultList = await _context.CommentsDb.ToListAsync();
            return Ok(resultList);
        }

        // GET api/comments/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            Comment result = await _context.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == id);
            if (result != null)
            {
                return Ok(result);
            }
            
            return NotFound();
        }

        // POST api/comments
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Comment model)
        {
            Comment newComment = new Comment();
            newComment.Created = DateTime.UtcNow;
            newComment.Author = model.Author;
            newComment.CommentText = model.CommentText;
            newComment.CommentThreadNumber = model.CommentThreadNumber;
            newComment.DisplayName = model.DisplayName;

            await _context.CommentsDb.AddAsync(newComment);
            await _context.SaveChangesAsync();

            CommentThread cmntThread =
                await _context.CommentThreadsDb.SingleOrDefaultAsync(c => c.CommentThreadId == model.CommentThreadNumber);
            cmntThread.CommentsCount = cmntThread.CommentsCount + 1;
            _context.CommentThreadsDb.Update(cmntThread);

            await _context.SaveChangesAsync();

            return Ok(newComment);
        }

        // PUT api/comments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Comment value)
        {
            Comment comment = await _context.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == id);

            // Todo: more validation of the values
            if (comment == null)
            {
                return NotFound();
            }

            comment.CommentText = value.CommentText;
            comment.Author = value.Author;
            comment.DisplayName = value.DisplayName;
            comment.Created = value.Created;

            _context.CommentsDb.Update(comment);
            await _context.SaveChangesAsync();

            return Ok(comment);
        }

        // DELETE api/comments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Comment comment = await _context.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == id);
            if (comment != null)
            {
                CommentThread cmntThread =
                    await _context.CommentThreadsDb.SingleOrDefaultAsync(c => c.CommentThreadId == comment.CommentThreadNumber);
                if (cmntThread.CommentsCount > 0)
                {
                    cmntThread.CommentsCount = cmntThread.CommentsCount - 1;
                    _context.CommentThreadsDb.Update(cmntThread);
                    await _context.SaveChangesAsync();
                }
                
                _context.CommentsDb.Remove(comment);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }

        }
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {

            HttpClient commentsHttpClient = new HttpClient();

            commentsHttpClient.BaseAddress = new Uri("https://kinauna.com");
            commentsHttpClient.DefaultRequestHeaders.Accept.Clear();
            commentsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string commentsApiPath = "/api/azureexport/commentsexport";
            var commentsUri = "https://kinauna.com" + commentsApiPath;

            var commentsResponseString = await commentsHttpClient.GetStringAsync(commentsUri);

            CommentDto commentsList = JsonConvert.DeserializeObject<CommentDto>(commentsResponseString);
            List<Comment> addedComments = new List<Comment>();
            foreach (Comment comment in commentsList.CommentsList)
            {
                Comment tempComment = await _context.CommentsDb.SingleOrDefaultAsync(l => l.CommentId == comment.CommentId);
                if (tempComment == null)
                {
                    Comment newComment = new Comment();
                    newComment.Author = comment.Author;
                    newComment.CommentText = comment.CommentText;
                    newComment.CommentThreadNumber = comment.CommentThreadNumber;
                    newComment.Created = comment.Created;
                    newComment.DisplayName = comment.DisplayName;

                    await _context.CommentsDb.AddAsync(newComment);
                    addedComments.Add(newComment);
                }
            }

            foreach (CommentThread cThread in commentsList.CommentThreadsList)
            {
                CommentThread tempCommentThread = await _context.CommentThreadsDb.SingleOrDefaultAsync(l => l.CommentThreadId == cThread.CommentThreadId);

                if (tempCommentThread == null)
                {
                    CommentThread newCommentThread = new CommentThread();
                    newCommentThread.CommentThreadId = cThread.CommentThreadId;
                    newCommentThread.CommentsCount = cThread.CommentsCount;

                    await _context.CommentThreadsDb.AddAsync(newCommentThread);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(addedComments);
        }
    }
}
