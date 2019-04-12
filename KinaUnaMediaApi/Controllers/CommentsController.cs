using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaMediaApi.Services;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly MediaDbContext _context;
        private readonly IDataService _dataService;

        public CommentsController(MediaDbContext context, IDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }

        // GET api/comments/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            Comment result = await _dataService.GetComment(id); // await _context.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == id);
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
            await _dataService.SetCommentsList(cmntThread.CommentThreadId);

            await _context.SaveChangesAsync();
            await _dataService.SetComment(newComment.CommentId);

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
            await _dataService.SetComment(comment.CommentId);
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
                    await _dataService.SetCommentsList(cmntThread.CommentThreadId);
                }
                
                _context.CommentsDb.Remove(comment);
                await _context.SaveChangesAsync();
                await _dataService.RemoveComment(comment.CommentId, comment.CommentThreadNumber);
                return NoContent();
            }
            else
            {
                return NotFound();
            }

        }
        
    }
}
