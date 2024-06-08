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
    public class CommentsController(
        IImageStore imageStore,
        IAzureNotifications azureNotifications,
        ICommentsService commentsService,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/comments/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetComment(int id)
        {
            Comment result = await commentsService.GetComment(id);
            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();

        }

        // GET api/comments/GetCommentsByThread/5
        [HttpGet]
        [Route("[action]/{threadId:int}")]
        public async Task<IActionResult> GetCommentsByThread(int threadId)
        {
            List<Comment> result = await commentsService.GetCommentsList(threadId);
            if (result == null) return NotFound();

            foreach (Comment comment in result)
            {
                UserInfo commentAuthor = await userInfoService.GetUserInfoByUserId(comment.Author);
                if (commentAuthor == null) continue;

                string authorImg = commentAuthor.ProfilePicture ?? "";
                comment.AuthorImage = imageStore.UriFor(authorImg, "profiles");
                comment.DisplayName = commentAuthor.FullName();
            }

            return Ok(result);

        }

        // POST api/comments
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Comment model)
        {
            Progeny progeny = await progenyService.GetProgeny(model.Progeny.Id);

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

            Comment newComment = await commentsService.AddComment(model);
            
            newComment.Progeny = progeny;
            string notificationTitle = "New comment for " + newComment.Progeny.NickName;
            string notificationMessage = model.DisplayName + " added a new comment for " + newComment.Progeny.NickName;

            TimeLineItem timeLineItem = new()
            {
                ProgenyId = newComment.Progeny.Id,
                ItemId = newComment.ItemId,
                ItemType = newComment.ItemType,
                AccessLevel = newComment.AccessLevel
            };

            UserInfo userinfo = await userInfoService.GetUserInfoByUserId(model.Author);

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userinfo.ProfilePicture);
            await webNotificationsService.SendCommentNotification(newComment, userinfo, notificationTitle, notificationMessage);

            return Ok(newComment);
        }

        // PUT api/comments/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Comment value)
        {
            Comment comment = await commentsService.GetComment(id);
            if (comment == null)
            {
                return NotFound();
            }

            Progeny progeny = await progenyService.GetProgeny(value.Progeny.Id);

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

            comment = await commentsService.UpdateComment(value);

            return Ok(comment);
        }

        // DELETE api/comments/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {

            Comment comment = await commentsService.GetComment(id);
            if (comment == null) return NotFound();

            string userId = User.GetUserId();
            if (userId != comment.Author)
            {
                return Unauthorized();
            }

            _ = await commentsService.DeleteComment(comment);
            
            return NoContent();

        }
    }
}
