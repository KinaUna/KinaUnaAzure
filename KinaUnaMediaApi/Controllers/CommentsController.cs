using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
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
        private readonly IDataService _dataService;
        private readonly ICommentsService _commentsService;
        private readonly ImageStore _imageStore;
        private readonly AzureNotifications _azureNotifications;

        public CommentsController(IDataService dataService, ImageStore imageStore, AzureNotifications azureNotifications, ICommentsService commentsService)
        {
            _dataService = dataService;
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _commentsService = commentsService;
        }

        // GET api/comments/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComment(int id)
        {
            Comment result = await _commentsService.GetComment(id);
            if (result != null)
            {
                return Ok(result);
            }
            
            return NotFound();
            
        }

        // GET api/comments/getcommentsbythread/5
        [HttpGet]
        [Route("[action]/{threadId}")]
        public async Task<IActionResult> GetCommentsByThread(int threadId)
        {
            List<Comment> result = await _commentsService.GetCommentsList(threadId);
            if (result != null)
            {
                foreach (Comment comment in result)
                {
                    UserInfo commentAuthor = await _dataService.GetUserInfoByUserId(comment.Author);
                    if (commentAuthor != null)
                    {
                        string authorImg = commentAuthor.ProfilePicture ?? "";
                        if (!string.IsNullOrEmpty(authorImg))
                        {
                            if (!authorImg.ToLower().StartsWith("http"))
                            {
                                authorImg = _imageStore.UriFor(authorImg, "profiles");
                            }
                        }

                        comment.AuthorImage = authorImg;
                        if (string.IsNullOrEmpty(comment.AuthorImage))
                        {
                            comment.AuthorImage = Constants.ProfilePictureUrl;
                        }
                        
                        comment.DisplayName = commentAuthor.FullName();
                    }
                }
                return Ok(result);
            }

            return NotFound();
        }

        // POST api/comments
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Comment model)
        {
            Progeny progeny = await _dataService.GetProgeny(model.Progeny.Id);
            
            string userId = User.GetUserId();
            if (progeny != null)
            {
                if (userId != model.Author)
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Comment newComment = await _commentsService.AddComment(model);
            await _commentsService.SetComment(newComment.CommentId);

            newComment.Progeny = progeny;
            string title = "New comment for " + newComment.Progeny.NickName;
            string message = model.DisplayName + " added a new comment for " + newComment.Progeny.NickName;
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = newComment.Progeny.Id;
            tItem.ItemId = newComment.ItemId;
            tItem.ItemType = newComment.ItemType;
            tItem.AccessLevel = newComment.AccessLevel;
            UserInfo userinfo = await _dataService.GetUserInfoByUserId(model.Author);
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(newComment);
        }

        // PUT api/comments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Comment value)
        {
            Comment comment = await _commentsService.GetComment(id);
            if (comment == null)
            {
                return NotFound();
            }

            Progeny progeny = await _dataService.GetProgeny(value.Progeny.Id);

            string userId = User.GetUserId();
            if (progeny != null)
            {

                if (userId != comment.Author)
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            comment = await _commentsService.UpdateComment(value);
            
            await _commentsService.SetComment(comment.CommentId);

            return Ok(comment);
        }

        // DELETE api/comments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            Comment comment = await _commentsService.GetComment(id);
            if (comment != null)
            {
                string userId = User.GetUserId();
                if (userId != comment.Author)
                {
                    return Unauthorized();
                }

                _ = await _commentsService.DeleteComment(comment);
                await _commentsService.RemoveComment(comment.CommentId, comment.CommentThreadNumber);
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
