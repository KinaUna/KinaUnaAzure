using System.Collections.Generic;
using System.Linq;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaMediaApi.Services
{
    public class DataService: IDataService
    {
        private readonly ProgenyDbContext _context;
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public DataService(ProgenyDbContext context, MediaDbContext mediaContext, IDistributedCache cache)
        {
            _context = context;
            _mediaContext = mediaContext;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }
        
        public UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = _cache.GetString(Constants.AppName + "progenyuseraccess" + progenyId + userEmail);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = _context.UserAccessDb.SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                _cache.SetString(Constants.AppName + "progenyuseraccess" + progenyId + userEmail, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public Picture GetPicture(int id)
        {
            Picture picture;
            string cachedPicture = _cache.GetString(Constants.AppName + "picture" + id);
            if (!string.IsNullOrEmpty(cachedPicture))
            {
                picture = JsonConvert.DeserializeObject<Picture>(cachedPicture);
            }
            else
            {
                picture = _mediaContext.PicturesDb.AsNoTracking().SingleOrDefault(p => p.PictureId == id);
                _cache.SetString(Constants.AppName + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);
            }

            return picture;
        }

        public Picture SetPicture(int id)
        {
            Picture picture = _mediaContext.PicturesDb.AsNoTracking().SingleOrDefault(p => p.PictureId == id);
            _cache.SetString(Constants.AppName + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);
            if (picture != null)
            {
                SetPicturesList(picture.ProgenyId);

                SetCommentsList(picture.CommentThreadNumber);
            }

            return picture;
        }

        public void RemovePicture(int pictureId, int progenyId)
        {
            _cache.Remove(Constants.AppName + "picture" + pictureId);

            List<Picture> picturesList = SetPicturesList(progenyId);
        }

        public List<Picture> GetPicturesList(int progenyId)
        {
            List<Picture> picturesList;
            string cachedPicturesList = _cache.GetString(Constants.AppName + "pictureslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedPicturesList))
            {
                picturesList = JsonConvert.DeserializeObject<List<Picture>>(cachedPicturesList);
            }
            else
            {
                picturesList = _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);
            }

            return picturesList;
        }

        public List<Picture> SetPicturesList(int progenyId)
        {
            List<Picture> picturesList = _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);
            
            return picturesList;
        }

        public Video GetVideo(int id)
        {
            Video video;
            string cachedVideo = _cache.GetString(Constants.AppName + "video" + id);
            if (!string.IsNullOrEmpty(cachedVideo))
            {
                video = JsonConvert.DeserializeObject<Video>(cachedVideo);
            }
            else
            {
                video = _mediaContext.VideoDb.AsNoTracking().SingleOrDefault(v => v.VideoId == id);
                _cache.SetString(Constants.AppName + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            }

            return video;
        }

        public Video SetVideo(int id)
        {
            Video video = _mediaContext.VideoDb.AsNoTracking().SingleOrDefault(v => v.VideoId == id);
            _cache.SetString(Constants.AppName + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            if (video != null)
            {
                List<Video> videosList = SetVideosList(video.ProgenyId);
                List<Comment> commentsList = SetCommentsList(video.CommentThreadNumber);
            }

            return video;
        }

        public void RemoveVideo(int videoId, int progenyId)
        {
            _cache.Remove(Constants.AppName + "video" + videoId);

            List<Video> videosList = SetVideosList(progenyId);
        }

        public List<Video> GetVideosList(int progenyId)
        {
            List<Video> videosList;
            string cachedVideosList = _cache.GetString(Constants.AppName + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonConvert.DeserializeObject<List<Video>>(cachedVideosList);
            }
            else
            {
                videosList = _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);
            }

            return videosList;
        }

        public List<Video> SetVideosList(int progenyId)
        {
            List<Video> videosList = _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);

            return videosList;
        }

        public Comment GetComment(int commentId)
        {
            Comment comment;
            string cachedComment = _cache.GetString(Constants.AppName + "comment" + commentId);
            if (!string.IsNullOrEmpty(cachedComment))
            {
                comment = JsonConvert.DeserializeObject<Comment>(cachedComment);
            }
            else
            {
                comment = _mediaContext.CommentsDb.AsNoTracking().SingleOrDefault(c => c.CommentId == commentId);
                _cache.SetString(Constants.AppName + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            }

            return comment;
        }

        public Comment SetComment(int commentId)
        {
            Comment comment = _mediaContext.CommentsDb.AsNoTracking().SingleOrDefault(c => c.CommentId == commentId);
            _cache.SetString(Constants.AppName + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            if (comment != null)
            {
                SetCommentsList(comment.CommentThreadNumber);

                Picture picture =
                    _mediaContext.PicturesDb.SingleOrDefault(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                if (picture != null)
                {
                    SetPicture(picture.PictureId);
                    SetPicturesList(picture.ProgenyId);
                }
                else {
                    Video video =
                        _mediaContext.VideoDb.SingleOrDefault(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                    if (video != null)
                    {
                        SetVideo(video.VideoId);
                        SetVideosList(video.ProgenyId);
                    }
                }
            }

            return comment;
        }

        public void RemoveComment(int commentId, int commentThreadId)
        {
            _cache.Remove(Constants.AppName + "comment" + commentId);
            SetCommentsList(commentThreadId);

            Picture picture =
                _mediaContext.PicturesDb.SingleOrDefault(p => p.CommentThreadNumber == commentThreadId);
            if (picture != null)
            {
                SetPicture(picture.PictureId);
                SetPicturesList(picture.ProgenyId);
            }
            else
            {
                Video video =
                    _mediaContext.VideoDb.SingleOrDefault(p => p.CommentThreadNumber == commentThreadId);
                if (video != null)
                {
                    SetVideo(video.VideoId);
                    SetVideosList(video.ProgenyId);
                }
            }
        }

        public List<Comment> GetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList;
            string cachedCommentsList = _cache.GetString(Constants.AppName + "commentslist" + commentThreadId);
            if (!string.IsNullOrEmpty(cachedCommentsList))
            {
                commentsList = JsonConvert.DeserializeObject<List<Comment>>(cachedCommentsList);
            }
            else
            {
                commentsList = _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToList();
                _cache.SetString(Constants.AppName + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            }

            return commentsList;
        }

        public List<Comment> SetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList = _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToList();
            _cache.SetString(Constants.AppName + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            
            return commentsList;
        }

        public void RemoveCommentsList(int commentThreadId)
        {
            _cache.Remove(Constants.AppName + "commentslist" + commentThreadId);
        }
    }
}
