using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentsService _commentsService;
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IImageStore _imageStore;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public CommentsController(IImageStore imageStore, IAzureNotifications azureNotifications, ICommentsService commentsService, IProgenyService progenyService,
            IUserInfoService userInfoService, IWebNotificationsService webNotificationsService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _commentsService = commentsService;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _webNotificationsService = webNotificationsService;
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
                    UserInfo commentAuthor = await _userInfoService.GetUserInfoByUserId(comment.Author);
                    if (commentAuthor != null)
                    {
                        string authorImg = commentAuthor.ProfilePicture ?? "";
                        comment.AuthorImage = _imageStore.UriFor(authorImg, "profiles");
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
            Progeny progeny = await _progenyService.GetProgeny(model.Progeny.Id);

            string userId = User.GetUserId();
            if (progeny != null && model.CommentThreadNumber != 0)
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
            string notificationTitle = "New comment for " + newComment.Progeny.NickName;
            string notificationMessage = model.DisplayName + " added a new comment for " + newComment.Progeny.NickName;

            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.ProgenyId = newComment.Progeny.Id;
            timeLineItem.ItemId = newComment.ItemId;
            timeLineItem.ItemType = newComment.ItemType;
            timeLineItem.AccessLevel = newComment.AccessLevel;
            
            UserInfo userinfo = await _userInfoService.GetUserInfoByUserId(model.Author);
            
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userinfo.ProfilePicture);
            await _webNotificationsService.SendCommentNotification(newComment, userinfo, notificationTitle, notificationMessage);

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

            Progeny progeny = await _progenyService.GetProgeny(value.Progeny.Id);

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
