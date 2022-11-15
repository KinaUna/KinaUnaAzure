using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
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
        private readonly ImageStore _imageStore;
        private readonly AzureNotifications _azureNotifications;

        public CommentsController(ImageStore imageStore, AzureNotifications azureNotifications, ICommentsService commentsService, IProgenyService progenyService, IUserInfoService userInfoService)
        {
            _imageStore = imageStore;
            _azureNotifications = azureNotifications;
            _commentsService = commentsService;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
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
                    UserInfo cmntAuthor = await _userInfoService.GetUserInfoByUserId(comment.Author);
                    if (cmntAuthor != null)
                    {
                        string authorImg = cmntAuthor.ProfilePicture ?? "";
                        string authorName = "";
                        if (!String.IsNullOrEmpty(authorImg))
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

                        if (!String.IsNullOrEmpty(cmntAuthor.FirstName))
                        {
                            authorName = cmntAuthor.FirstName;
                        }
                        if (!String.IsNullOrEmpty(cmntAuthor.MiddleName))
                        {
                            authorName = authorName + " " + cmntAuthor.MiddleName;
                        }
                        if (!String.IsNullOrEmpty(cmntAuthor.LastName))
                        {
                            authorName = authorName + " " + cmntAuthor.LastName;
                        }

                        authorName = authorName.Trim();
                        if (String.IsNullOrEmpty(authorName))
                        {
                            authorName = cmntAuthor.UserName;
                            if (String.IsNullOrEmpty(authorName))
                            {
                                authorName = comment.DisplayName;
                            }
                        }

                        comment.DisplayName = authorName;
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
            Comment newComment = new Comment();
            newComment.Created = DateTime.UtcNow;
            newComment.Author = model.Author;
            newComment.CommentText = model.CommentText;
            newComment.CommentThreadNumber = model.CommentThreadNumber;
            newComment.DisplayName = model.DisplayName;

            newComment = await _commentsService.AddComment(newComment);
            await _commentsService.SetComment(newComment.CommentId);

            model.Progeny = await _progenyService.GetProgeny(model.Progeny.Id);
            string title = "New comment for " + model.Progeny.NickName;
            string message = model.DisplayName + " added a new comment for " + model.Progeny.NickName;
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = model.Progeny.Id;
            tItem.ItemId = model.ItemId;
            tItem.ItemType = model.ItemType;
            tItem.AccessLevel = model.AccessLevel;
            UserInfo userinfo = await _userInfoService.GetUserInfoByUserId(model.Author);
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(newComment);
        }

        // PUT api/comments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Comment value)
        {
            Comment comment = await _commentsService.GetComment(id);

            // Todo: more validation of the values
            if (comment == null)
            {
                return NotFound();
            }

            comment.CommentText = value.CommentText;
            comment.Author = value.Author;
            comment.DisplayName = value.DisplayName;
            comment.Created = value.Created;

            comment = await _commentsService.UpdateComment(comment);
            
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
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(User.GetEmail());
                if (userInfo.UserId != comment.Author)
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
