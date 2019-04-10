using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaMediaApi.Services
{
    public interface IDataService
    {
        UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Picture GetPicture(int id);
        Picture SetPicture(int id);
        void RemovePicture(int pictureId, int progenyId);
        List<Picture> GetPicturesList(int progenyId);
        List<Picture> SetPicturesList(int progenyId);
        Video GetVideo(int id);
        Video SetVideo(int id);
        void RemoveVideo(int videoId, int progenyId);
        List<Video> GetVideosList(int progenyId);
        List<Video> SetVideosList(int progenyId);
        Comment GetComment(int commentId);
        Comment SetComment(int commentId);
        void RemoveComment(int commentId, int commentThreadId);
        List<Comment> GetCommentsList(int commentThreadId);
        List<Comment> SetCommentsList(int commentThreadId);
        void RemoveCommentsList(int commentThreadId);
    }
}
