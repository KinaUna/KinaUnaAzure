using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Services
{
    public interface IDataService
    {
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task<Picture> GetPicture(int id);
        Task<Picture> SetPicture(int id);
        Task RemovePicture(int pictureId, int progenyId);
        Task<List<Picture>> GetPicturesList(int progenyId);
        Task<List<Picture>> SetPicturesList(int progenyId);
        Task<Video> GetVideo(int id);
        Task<Video> SetVideo(int id);
        Task RemoveVideo(int videoId, int progenyId);
        Task<List<Video>> GetVideosList(int progenyId);
        Task<List<Video>> SetVideosList(int progenyId);
        Task<Comment> GetComment(int commentId);
        Task<Comment> SetComment(int commentId);
        Task RemoveComment(int commentId, int commentThreadId);
        Task<List<Comment>> GetCommentsList(int commentThreadId);
        Task<List<Comment>> SetCommentsList(int commentThreadId);
        Task RemoveCommentsList(int commentThreadId);
    }
}
