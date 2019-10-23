using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        public async Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + "progenyuseraccess" + progenyId + userEmail);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "progenyuseraccess" + progenyId + userEmail, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public async Task<Picture> GetPicture(int id)
        {
            Picture picture;
            string cachedPicture = await _cache.GetStringAsync(Constants.AppName + "picture" + id);
            if (!string.IsNullOrEmpty(cachedPicture))
            {
                picture = JsonConvert.DeserializeObject<Picture>(cachedPicture);
            }
            else
            {
                picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
                await _cache.SetStringAsync(Constants.AppName + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);
            }

            return picture;
        }

        public async Task<Picture> SetPicture(int id)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            await _cache.SetStringAsync(Constants.AppName + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);
            if (picture != null)
            {
                await SetPicturesList(picture.ProgenyId);

                await SetCommentsList(picture.CommentThreadNumber);
            }

            return picture;
        }

        public async Task RemovePicture(int pictureId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "picture" + pictureId);

            await SetPicturesList(progenyId);
        }

        public async Task<List<Picture>> GetPicturesList(int progenyId)
        {
            List<Picture> picturesList;
            string cachedPicturesList = await _cache.GetStringAsync(Constants.AppName + "pictureslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedPicturesList))
            {
                picturesList = JsonConvert.DeserializeObject<List<Picture>>(cachedPicturesList);
            }
            else
            {
                picturesList = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);
            }

            return picturesList;
        }

        public async Task<List<Picture>> SetPicturesList(int progenyId)
        {
            List<Picture> picturesList = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);
            
            return picturesList;
        }

        public async Task<Video> GetVideo(int id)
        {
            Video video;
            string cachedVideo = await _cache.GetStringAsync(Constants.AppName + "video" + id);
            if (!string.IsNullOrEmpty(cachedVideo))
            {
                video = JsonConvert.DeserializeObject<Video>(cachedVideo);
            }
            else
            {
                video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);
                await _cache.SetStringAsync(Constants.AppName + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            }

            return video;
        }

        public async Task<Video> SetVideo(int id)
        {
            Video video = await _mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == id);
            await _cache.SetStringAsync(Constants.AppName + "video" + id, JsonConvert.SerializeObject(video), _cacheOptionsSliding);
            if (video != null)
            {
                await SetVideosList(video.ProgenyId);
                await SetCommentsList(video.CommentThreadNumber);
            }

            return video;
        }

        public async Task RemoveVideo(int videoId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "video" + videoId);

            await SetVideosList(progenyId);
        }

        public async Task<List<Video>> GetVideosList(int progenyId)
        {
            List<Video> videosList;
            string cachedVideosList = await _cache.GetStringAsync(Constants.AppName + "videoslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVideosList))
            {
                videosList = JsonConvert.DeserializeObject<List<Video>>(cachedVideosList);
            }
            else
            {
                videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);
            }

            return videosList;
        }

        public async Task<List<Video>> SetVideosList(int progenyId)
        {
            List<Video> videosList = await _mediaContext.VideoDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "videoslist" + progenyId, JsonConvert.SerializeObject(videosList), _cacheOptionsSliding);

            return videosList;
        }

        public async Task<Comment> GetComment(int commentId)
        {
            Comment comment;
            string cachedComment = await _cache.GetStringAsync(Constants.AppName + "comment" + commentId);
            if (!string.IsNullOrEmpty(cachedComment))
            {
                comment = JsonConvert.DeserializeObject<Comment>(cachedComment);
            }
            else
            {
                comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
                await _cache.SetStringAsync(Constants.AppName + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            }

            return comment;
        }

        public async Task<Comment> SetComment(int commentId)
        {
            Comment comment = await _mediaContext.CommentsDb.AsNoTracking().SingleOrDefaultAsync(c => c.CommentId == commentId);
            _cache.SetString(Constants.AppName + "comment" + commentId, JsonConvert.SerializeObject(comment), _cacheOptionsSliding);
            if (comment != null)
            {
                await SetCommentsList(comment.CommentThreadNumber);

                Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                if (picture != null)
                {
                    await SetPicture(picture.PictureId);
                    await SetPicturesList(picture.ProgenyId);
                }
                else {
                    Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == comment.CommentThreadNumber);
                    if (video != null)
                    {
                        await SetVideo(video.VideoId);
                        await SetVideosList(video.ProgenyId);
                    }
                }
            }

            return comment;
        }

        public async Task RemoveComment(int commentId, int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + "comment" + commentId);
            await SetCommentsList(commentThreadId);

            Picture picture = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
            if (picture != null)
            {
                await SetPicture(picture.PictureId);
                await SetPicturesList(picture.ProgenyId);
            }
            else
            {
                Video video = await _mediaContext.VideoDb.SingleOrDefaultAsync(p => p.CommentThreadNumber == commentThreadId);
                if (video != null)
                {
                    await SetVideo(video.VideoId);
                    await SetVideosList(video.ProgenyId);
                }
            }
        }

        public async Task<List<Comment>> GetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList;
            string cachedCommentsList = await _cache.GetStringAsync(Constants.AppName + "commentslist" + commentThreadId);
            if (!string.IsNullOrEmpty(cachedCommentsList))
            {
                commentsList = JsonConvert.DeserializeObject<List<Comment>>(cachedCommentsList);
            }
            else
            {
                commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            }

            return commentsList;
        }

        public async Task<List<Comment>> SetCommentsList(int commentThreadId)
        {
            List<Comment> commentsList = await _mediaContext.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == commentThreadId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "commentslist" + commentThreadId, JsonConvert.SerializeObject(commentsList), _cacheOptionsSliding);
            
            return commentsList;
        }

        public async Task RemoveCommentsList(int commentThreadId)
        {
            await _cache.RemoveAsync(Constants.AppName + "commentslist" + commentThreadId);
        }

        public async Task<UserInfo> GetUserInfoByUserId(string id)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + "userinfobyuserid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserId.ToUpper() == id.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "userinfobyuserid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<UserInfo> GetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + "userinfobymail" + userEmail);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarList;
        }

        public async Task<List<Location>> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return locationsList;
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList;
            string cachedFriendsList = await _cache.GetStringAsync(Constants.AppName + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }
            else
            {
                friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
            }

            return friendsList;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }

            return contactsList;
        }
    }
}
