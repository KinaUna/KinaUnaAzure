using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class CommentsService : ICommentsService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IPicturesService _picturesService;
        private readonly IVideosService _videosService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public CommentsService(MediaDbContext mediaContext, IDistributedCache cache, IPicturesService picturesService, IVideosService videosService)
        {
            _mediaContext = mediaContext;
            _cache = cache;
            _ = _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _ = _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 96 hours.
            _picturesService = picturesService;
            _videosService = videosService;
        }

        /// <summary>
        /// Gets a Comment from the cache.
        /// If it isn't in the cache gets it from the database.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to get.</param>
        /// <returns>Comment.</returns>
        public async Task<Comment> GetComment(int commentId)
        {
            Comment comment;
            string cachedComment = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId);
            if (!string.IsNullOrEmpty(cachedComment))
            {
                comment = JsonConvert.DeserializeObject<Comment>(cachedComment);
            }
            else
            {
                comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            }

            return comment;
        }

        /// <summary>
        /// Sets a Comment in the cache.
        /// Also updates the caches for the Picture or Video it belongs to.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to set in the cache.</param>
        /// <returns>The Comment with the given CommentId. Null if it doesn't exist.</returns>
        private async Task<Comment> SetComment(int commentId)
        {
            Comment comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            if (comment == null) return null;

            _ = await SetCommentsList(comment.CommentThreadNumber);

            Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
            if (picture != null)
            {
                _ = await _picturesService.SetPictureInCache(picture.PictureId);
                _ = await _picturesService.SetPicturesListInCache(picture.ProgenyId);
            }
            else
            {
                Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                if (video == null) return null;

                _ = await _videosService.SetVideoInCache(video.VideoId);
                _ = await _videosService.SetVideosListInCache(video.ProgenyId);
            }

            return comment;
        }

        /// <summary>
        /// Removes a Comment from the cache.
        /// Updates the cache for the Picture or Video it belongs to.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to remove.</param>
        /// <param name="commentThreadId">The CommentThreadId of the Comment to remove.</param>
        /// <returns></returns>
        public async Task RemoveComment(int commentId, int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId);
            _ = await SetCommentsList(commentThreadId);

            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
            if (picture != null)
            {
                _ = await _picturesService.SetPictureInCache(picture.PictureId);
                _ = await _picturesService.SetPicturesListInCache(picture.ProgenyId);
            }
            else
            {
                Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
                if (video != null)
                {
                    _ = await _videosService.SetVideoInCache(video.VideoId);
                    _ = await _videosService.SetVideosListInCache(video.ProgenyId);
                }
            }
        }

        /// <summary>
        /// Gets a List of Comments for a CommentThread from the cache.
        /// If the list isn't found in the cache, gets it from the database and sets it in the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId to get Comments for.</param>
        /// <returns>List of Comments.</returns>
        public async Task<List<Comment>> GetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList;
            string cachedCommentsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId);
            if (!string.IsNullOrEmpty(cachedCommentsList))
            {
                commentsList = JsonConvert.DeserializeObject<List<Comment>>(cachedCommentsList);
            }
            else
            {
                commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            }

            return commentsList;
        }

        /// <summary>
        /// Gets and sets a List of Comments for a CommentThread in the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId to get Comments for.</param>
        /// <returns>List of Comments.</returns>
        public async Task<List<Comment>> SetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);

            return commentsList;
        }

        /// <summary>
        /// Removes a List of Comments for a CommentThread from the cache.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId of the cached list to remove.</param>
        /// <returns></returns>
        public async Task RemoveCommentsList(int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId);
        }

        /// <summary>
        /// Adds a new Comment to the database.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment to add.</param>
        /// <returns>The added Comment.</returns>
        public async Task<Comment> AddComment(Comment comment)
        {
            Comment commentToAdd = new();
            commentToAdd.CopyPropertiesForAdd(comment);

            _ = _mediaContext.CommentsDb.Add(commentToAdd);
            _ = await _mediaContext.SaveChangesAsync();

            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == commentToAdd.CommentThreadNumber);
            if (cmntThread == null) return commentToAdd;

            cmntThread.CommentsCount += 1;
            _ = _mediaContext.CommentThreadsDb.Update(cmntThread);
            _ = await _mediaContext.SaveChangesAsync();
            _ = await SetComment(commentToAdd.CommentId);
            _ = await SetCommentsList(cmntThread.Id);

            return commentToAdd;
        }

        /// <summary>
        /// Updates a Comment in the database and cache.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment with updated properties.</param>
        /// <returns>The updated Comment.</returns>
        public async Task<Comment> UpdateComment(Comment comment)
        {
            Comment commentToUpdate = await _mediaContext.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == comment.CommentId);
            if (commentToUpdate == null) return null;

            commentToUpdate.CopyPropertiesForUpdate(comment);
            _ = _mediaContext.CommentsDb.Update(commentToUpdate);

            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetComment(comment.CommentId);

            return commentToUpdate;
        }

        /// <summary>
        /// Deletes a Comment from the database and removes it from the cache.
        /// Then updates the CommentThread and the cache for the CommentThread.
        /// </summary>
        /// <param name="comment">The Comment to delete.</param>
        /// <returns>The deleted Comment.</returns>
        public async Task<Comment> DeleteComment(Comment comment)
        {
            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == comment.CommentThreadNumber);

            Comment commentToRemove = await _mediaContext.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == comment.CommentId);
            _ = _mediaContext.CommentsDb.Remove(commentToRemove);

            await RemoveComment(comment.CommentId, comment.CommentThreadNumber);

            _ = await _mediaContext.SaveChangesAsync();

            if (cmntThread == null || cmntThread.CommentsCount <= 0) return null;

            cmntThread.CommentsCount -= 1;
            _ = _mediaContext.CommentThreadsDb.Update(cmntThread);
            _ = await _mediaContext.SaveChangesAsync();
            _ = await SetCommentsList(cmntThread.Id);

            return comment;
        }

        /// <summary>
        /// Gets a CommentThread entity from the database.
        /// </summary>
        /// <param name="commentThreadId">The CommentThreadId of the CommentThread entity to get.</param>
        /// <returns>CommentThread object.</returns>
        public async Task<CommentThread> GetCommentThread(int commentThreadId)
        {
            CommentThread commentThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == commentThreadId);
            return commentThread;
        }

        /// <summary>
        /// Adds a new CommentThread entity to the database.
        /// </summary>
        /// <returns>The added CommentThread object.</returns>
        public async Task<CommentThread> AddCommentThread()
        {
            CommentThread commentThread = new();
            _ = _mediaContext.CommentThreadsDb.Add(commentThread);
            _ = await _mediaContext.SaveChangesAsync();

            return commentThread;
        }

        /// <summary>
        /// Deletes a CommentThread entity from the database.
        /// </summary>
        /// <param name="commentThread">The CommentThread entity to delete.</param>
        /// <returns>The deleted CommentThread entity.</returns>
        public async Task<CommentThread> DeleteCommentThread(CommentThread commentThread)
        {
            _ = _mediaContext.CommentThreadsDb.Remove(commentThread);
            _ = await _mediaContext.SaveChangesAsync();

            return commentThread;
        }
    }
}
