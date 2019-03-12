using System.Collections.Generic;
using KinaUnaWeb.Models.ItemViewModels;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface IMediaHttpClient
    {
        Task<HttpClient> GetClient();
        Task<Picture> GetPicture(int pictureId, string timeZone);
        Task<Picture> GetRandomPicture(int progenyId, int accessLevel, string timeZone);
        Task<List<Picture>> GetPictureList(int progenyId, int accessLevel, string timeZone);
        Task<List<Picture>> GetAllPictures();
        Task<Picture> AddPicture(Picture picture);
        Task<Picture> UpdatePicture(Picture picture);
        Task<bool> DeletePicture(int pictureId);
        Task<bool> AddPictureComment(Comment comment);
        Task<bool> DeletePictureComment(int commentId);

        Task<PicturePageViewModel> GetPicturePage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        Task<PictureViewModel> GetPictureViewModel(int id, int userAccessLevel, int sortBy, string timeZone);

        Task<Video> GetVideo(int videoId, string timeZone);
        Task<List<Video>> GetVideoList(int progenyId, int accessLevel, string timeZone);
        Task<List<Video>> GetAllVideos();
        Task<Video> AddVideo(Video video);
        Task<Video> UpdateVideo(Video video);
        Task<bool> DeleteVideo(int videoId);
        Task<bool> AddVideoComment(Comment comment);
        Task<bool> DeleteVideoComment(int commentId);
        Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        Task<VideoViewModel> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone);
    }
}
