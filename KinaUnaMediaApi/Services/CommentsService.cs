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

namespace KinaUnaMediaApi.Services
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

        public async Task<Comment> SetComment(int commentId)
        {
            Comment comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            if (comment != null)
            {
                await SetCommentsList(comment.CommentThreadNumber);
                
                Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                if (picture != null) 
                {
                    await _picturesService.SetPicture(picture.PictureId);
                    await _picturesService.SetPicturesList(picture.ProgenyId);
                }
                else
                {
                    Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                    if (video != null)
                    {
                        await _videosService.SetVideo(video.VideoId);
                        await _videosService.SetVideosList(video.ProgenyId);
                    }
                }
            }

            return comment;
        }

        public async Task RemoveComment(int commentId, int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "comment" + commentId);
            await SetCommentsList(commentThreadId);

            Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
            if (picture != null)
            {
                await _picturesService.SetPicture(picture.PictureId);
                await _picturesService.SetPicturesList(picture.ProgenyId);
            }
            else
            {
                Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
                if (video != null)
                {
                    await _videosService.SetVideo(video.VideoId);
                    await _videosService.SetVideosList(video.ProgenyId);
                }
            }
        }

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

        public async Task<List<Comment>> SetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            
            return commentsList;
        }

        public async Task RemoveCommentsList(int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "commentslist" + commentThreadId);
        }

        public async Task<Comment> AddComment(Comment comment)
        {
            Comment commentToAdd = new Comment();
            commentToAdd.CopyPropertiesForAdd(comment);

            await _mediaContext.CommentsDb.AddAsync(commentToAdd);
            await _mediaContext.SaveChangesAsync();

            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == commentToAdd.CommentThreadNumber);
            if (cmntThread != null)
            {
                cmntThread.CommentsCount += 1;
                _mediaContext.CommentThreadsDb.Update(cmntThread);
                await SetCommentsList(cmntThread.Id);
            }

            return commentToAdd;
        }

        public async Task<Comment> UpdateComment(Comment comment)
        {
            Comment commentToUpdate = await _mediaContext.CommentsDb.SingleOrDefaultAsync(c => c.CommentId == comment.CommentId);
            if (commentToUpdate != null)
            {
                commentToUpdate.CopyPropertiesForUpdate(comment);
                _mediaContext.CommentsDb.Update(commentToUpdate);

                await _mediaContext.SaveChangesAsync();
            }
            
            return commentToUpdate;
        }

        public async Task<Comment> DeleteComment(Comment comment)
        {
            CommentThread cmntThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == comment.CommentThreadNumber);

            _mediaContext.CommentsDb.Remove(comment);
            await _mediaContext.SaveChangesAsync();

            if (cmntThread != null && cmntThread.CommentsCount > 0)
            {
                cmntThread.CommentsCount -= 1;
                _mediaContext.CommentThreadsDb.Update(cmntThread);
                await _mediaContext.SaveChangesAsync();
                await SetCommentsList(cmntThread.Id);
            }
            
            return comment;
        }

        public async Task<CommentThread> GetCommentThread(int commentThreadId)
        {
            CommentThread commentThread = await _mediaContext.CommentThreadsDb.SingleOrDefaultAsync(c => c.Id == commentThreadId);
            return commentThread;
        }

        public async Task<CommentThread> AddCommentThread()
        {
            CommentThread commentThread = new CommentThread();
            await _mediaContext.CommentThreadsDb.AddAsync(commentThread);
            await _mediaContext.SaveChangesAsync();

            return commentThread;
        }

        public async Task<CommentThread> DeleteCommentThread(CommentThread commentThread)
        {
            _mediaContext.CommentThreadsDb.Remove(commentThread);
            await _mediaContext.SaveChangesAsync();

            return commentThread;
        }
    }
}
