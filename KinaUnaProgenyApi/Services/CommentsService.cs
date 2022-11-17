using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class CommentsService: ICommentsService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IPicturesService _picturesService;
        private readonly IVideosService _videosService;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public CommentsService(MediaDbContext mediaContext, IDistributedCache cache, IPicturesService picturesService, IVideosService videosService)
        {
            _mediaContext = mediaContext;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 24 hours.
            _picturesService = picturesService;
            _videosService = videosService;
        }

        public async Task<Comment> GetComment(int commentId)
        {
            Comment comment = await GetCommentFromCache(commentId);
            if (comment == null || comment.CommentId == 0)
            {
                comment = await SetCommentInCache(commentId);
            }

            return comment;
        }

        private async Task<Comment> GetCommentFromCache(int commentId)
        {
            Comment comment = new Comment();
            string cachedComment = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId);
            if (!string.IsNullOrEmpty(cachedComment))
            {
                comment = JsonConvert.DeserializeObject<Comment>(cachedComment);
            }

            return comment;
        }

        public async Task<Comment> SetCommentInCache(int commentId)
        {
            Comment comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
            
            if (comment != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
                _ = await SetCommentsListInCache(comment.CommentThreadNumber);
                
                Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                if (picture != null) 
                {
                    _ = await _picturesService.SetPicture(picture.PictureId);
                    _ = await _picturesService.SetPicturesList(picture.ProgenyId);
                }
                else
                {
                    Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                    if (video != null)
                    {
                        _ = await _videosService.SetVideo(video.VideoId);
                        _ = await _videosService.SetVideosList(video.ProgenyId);
                    }
                }
            }

            return comment;
        }

        public async Task RemoveCommentFromCache(int commentId, int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId);
            _ = await SetCommentsListInCache(commentThreadId);

            Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
            if (picture != null)
            {
                _ = await _picturesService.SetPicture(picture.PictureId);
                _ = await _picturesService.SetPicturesList(picture.ProgenyId);
            }
            else
            {
                Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
                if (video != null)
                {
                    _ = await _videosService.SetVideo(video.VideoId);
                    _ = await _videosService.SetVideosList(video.ProgenyId);
                }
            }
        }

        public async Task<List<Comment>> GetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList = await GetCommentListFromCache(commentThreadId);
            if (commentsList == null || commentsList.Count == 0)
            {
                commentsList = await SetCommentsListInCache(commentThreadId);
            }

            return commentsList;
        }

        private async Task<List<Comment>> GetCommentListFromCache(int commentThreadId)
        {
            List<Comment> commentsList = new List<Comment>();
            string cachedCommentsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId);
            if (!string.IsNullOrEmpty(cachedCommentsList))
            {
                commentsList = JsonConvert.DeserializeObject<List<Comment>>(cachedCommentsList);
            }

            return commentsList;
        }

        public async Task<List<Comment>> SetCommentsListInCache(int commentThreadId)
        {
            List<Comment> commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            
            return commentsList;
        }

        public async Task RemoveCommentsListFromCache(int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId);
        }

        public async Task<Comment> AddComment(Comment comment)
        {
            _ = await _mediaContext.CommentsDb.AddAsync(comment);
            _ = await _mediaContext.SaveChangesAsync();

            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == comment.CommentThreadNumber);
            if (cmntThread != null)
            {
                cmntThread.CommentsCount += 1;
                _ = _mediaContext.CommentThreadsDb.Update(cmntThread);
                _ = await SetCommentsListInCache(cmntThread.Id);
            }

            return comment;
        }

        public async Task<Comment> UpdateComment(Comment comment)
        {
            Comment commentToUpdate = await _mediaContext.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == comment.CommentId);
            if (commentToUpdate != null)
            {
                commentToUpdate.Author = comment.Author;
                commentToUpdate.AccessLevel = comment.AccessLevel;
                commentToUpdate.Progeny = comment.Progeny;
                commentToUpdate.AuthorImage = comment.AuthorImage;
                commentToUpdate.CommentText = comment.CommentText;
                commentToUpdate.CommentThreadNumber = comment.CommentThreadNumber;
                commentToUpdate.Created = comment.Created;
                commentToUpdate.DisplayName = comment.DisplayName;
                commentToUpdate.ItemId = comment.ItemId;
                commentToUpdate.ItemType = comment.ItemType;

                _ = _mediaContext.CommentsDb.Update(commentToUpdate);
                _ = await _mediaContext.SaveChangesAsync();
            }
            
            return commentToUpdate;
        }

        public async Task<Comment> DeleteComment(Comment comment)
        {
            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == comment.CommentThreadNumber);

            Comment commentToRemove = await _mediaContext.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == comment.CommentId);
            if (commentToRemove != null)
            {
                _ = _mediaContext.CommentsDb.Remove(comment);
                _ = await _mediaContext.SaveChangesAsync();

                if (cmntThread != null && cmntThread.CommentsCount > 0)
                {
                    cmntThread.CommentsCount -= 1;
                    _ = _mediaContext.CommentThreadsDb.Update(cmntThread);
                    _ = await _mediaContext.SaveChangesAsync();
                    _ = await SetCommentsListInCache(cmntThread.Id);
                }
            }
            
            
            return commentToRemove;
        }

        public async Task<CommentThread> GetCommentThread(int commentThreadId)
        {
            CommentThread commentThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == commentThreadId);
            return commentThread;
        }

        public async Task<CommentThread> AddCommentThread()
        {
            CommentThread commentThread = new CommentThread();
            _ = await _mediaContext.CommentThreadsDb.AddAsync(commentThread);
            _ = await _mediaContext.SaveChangesAsync();

            return commentThread;
        }

        public async Task<CommentThread> DeleteCommentThread(CommentThread commentThread)
        {
            _ = _mediaContext.CommentThreadsDb.Remove(commentThread);
            _ = await _mediaContext.SaveChangesAsync();

            return commentThread;
        }
    }
}
