using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for comments.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="commentsService"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController(
        IAzureNotifications azureNotifications,
        ICommentsService commentsService,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves the Comment with a given id.
        /// </summary>
        /// <param name="id">The CommentId of the Comment.</param>
        /// <returns>The Comment with the provided CommentId.</returns>
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

        /// <summary>
        /// Retrieves all Comments for a given comment thread.
        /// </summary>
        /// <param name="threadId">The id/CommentThreadNumber of the comment thread.</param>
        /// <returns>List of Comments with the provided CommentThreadNumber.</returns>
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

                comment.AuthorImage = commentAuthor.GetProfilePictureUrl();
                comment.DisplayName = commentAuthor.FullName();
            }

            return Ok(result);

        }

        /// <summary>
        /// Adds a new Comment to the database.
        /// Then sends notifications to users who have access to the Comment.
        /// </summary>
        /// <param name="value">The comment object to add</param>
        /// <returns>The added comment if the user has access to the Progeny.</returns>
        // POST api/comments
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Comment value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.Progeny.Id);

            string userId = User.GetUserId();
            // Todo: Check user's access level to the Progeny.
            if (progeny != null && value.CommentThreadNumber != 0)
            {
                if (userId != value.Author)
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Comment newComment = await commentsService.AddComment(value);
            
            newComment.Progeny = progeny;
            string notificationTitle = "New comment for " + newComment.Progeny.NickName;
            string notificationMessage = value.DisplayName + " added a new comment for " + newComment.Progeny.NickName;

            TimeLineItem timeLineItem = new()
            {
                ProgenyId = newComment.Progeny.Id,
                ItemId = newComment.ItemId,
                ItemType = newComment.ItemType,
                AccessLevel = newComment.AccessLevel
            };

            UserInfo userinfo = await userInfoService.GetUserInfoByUserId(value.Author);

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userinfo.ProfilePicture);
            await webNotificationsService.SendCommentNotification(newComment, userinfo, notificationTitle, notificationMessage);

            return Ok(newComment);
        }

        /// <summary>
        /// Updates an existing Comment in the database.
        /// </summary>
        /// <param name="id">The CommentId of the Comment to update.</param>
        /// <param name="value">The Comment object with the updated properties.</param>
        /// <returns>The updated Comment, if the user has access to it, else UnauthorizedResult. If it is not found a NotFoundResult is returned.</returns>
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

        /// <summary>
        /// Deletes a Comment from the database.
        /// </summary>
        /// <param name="id">The CommentId of the Comment entity to remove.</param>
        /// <returns>NoContentResult of the Comment was removed, UnauthorizedResult if the user doesn't have access to this comment, NotFoundResult if the Comment doesn't exist.</returns>
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
